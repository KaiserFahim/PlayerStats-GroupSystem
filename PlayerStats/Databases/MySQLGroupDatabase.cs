using Dapper;
using MySql.Data.MySqlClient;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.Databases
{
    internal class MySQLGroupDatabase : IGroupDatabase
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;
        private MySqlConnection connection => new(configuration.MySQLConnectionString);

        private string FormatSql(string query)
        {
            return query
                .Replace("PlayerStats", configuration.PlayerStatsTableName)
                .Replace("playerstats_groups", configuration.PlayerStatsTableName + "_groups")
                .Replace("playerstats_group_invites", configuration.PlayerStatsTableName + "_group_invites");
        }

        public void Initialize()
        {
            using (MySqlConnection conn = connection)
            {
                conn.Execute(FormatSql(@"
                    CREATE TABLE IF NOT EXISTS playerstats_groups (
                        GroupId VARCHAR(36) NOT NULL,
                        GroupName VARCHAR(64) NOT NULL UNIQUE,
                        OwnerSteamId BIGINT UNSIGNED NOT NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT PK_GroupId PRIMARY KEY (GroupId)
                    )"));

                conn.Execute(FormatSql(@"
                    CREATE TABLE IF NOT EXISTS playerstats_group_invites (
                        SteamId BIGINT UNSIGNED NOT NULL,
                        GroupId VARCHAR(36) NOT NULL,
                        ExpiresAt DATETIME NOT NULL,
                        CONSTRAINT PK_Invite PRIMARY KEY (SteamId, GroupId)
                    )"));

                const string groupIdCheck = @"
                    SELECT COUNT(*) FROM information_schema.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PlayerStats' AND COLUMN_NAME = 'GroupId'";

                if (conn.ExecuteScalar<int>(FormatSql(groupIdCheck)) == 0)
                {
                    Logger.Log($"Adding GroupId column to {configuration.PlayerStatsTableName}...");
                    conn.Execute(FormatSql("ALTER TABLE PlayerStats ADD COLUMN GroupId VARCHAR(36) NULL"));
                    Logger.Log("GroupId column added successfully!");
                }
            }
        }

        public void Save()
        {
            // MySQL saves are immediate — no-op for groups since we save on each operation
        }

        public Group GetGroup(string groupId)
        {
            string groupQuery = FormatSql("SELECT * FROM playerstats_groups WHERE GroupId = @groupId");
            string inviteQuery = FormatSql("SELECT SteamId, ExpiresAt FROM playerstats_group_invites WHERE GroupId = @groupId");
            string memberQuery = FormatSql("SELECT SteamId FROM PlayerStats WHERE GroupId = @groupId");

            using (MySqlConnection conn = connection)
            {
                dynamic row = conn.QueryFirstOrDefault(groupQuery, new { groupId });
                if (row == null) return null;

                List<ulong> members = conn.Query<ulong>(memberQuery, new { groupId }).ToList();
                List<dynamic> invites = conn.Query(inviteQuery, new { groupId }).ToList();

                Group group = new()
                {
                    GroupId = row.GroupId,
                    GroupName = row.GroupName,
                    OwnerSteamId = row.OwnerSteamId,
                    CreatedAt = row.CreatedAt,
                    Members = members
                };

                foreach (dynamic inv in invites)
                {
                    group.InvitedPlayers[(ulong)inv.SteamId] = inv.ExpiresAt;
                }

                return group;
            }
        }

        public Group GetGroupByMember(ulong steamId)
        {
            string query = FormatSql("SELECT GroupId FROM PlayerStats WHERE SteamId = @steamId AND GroupId IS NOT NULL");

            using (MySqlConnection conn = connection)
            {
                string groupId = conn.QueryFirstOrDefault<string>(query, new { steamId });
                if (string.IsNullOrEmpty(groupId)) return null;
                return GetGroup(groupId);
            }
        }

        public Group GetGroupByName(string name)
        {
            string query = FormatSql("SELECT GroupId FROM playerstats_groups WHERE GroupName = @name");

            using (MySqlConnection conn = connection)
            {
                string groupId = conn.QueryFirstOrDefault<string>(query, new { name });
                if (string.IsNullOrEmpty(groupId)) return null;
                return GetGroup(groupId);
            }
        }

        public List<Group> GetAllGroups()
        {
            string query = FormatSql("SELECT GroupId FROM playerstats_groups");

            using (MySqlConnection conn = connection)
            {
                List<string> groupIds = conn.Query<string>(query).ToList();
                return groupIds.Select(gid => GetGroup(gid)).Where(g => g != null).ToList();
            }
        }

        public void AddOrUpdateGroup(Group group)
        {
            string upsertGroup = FormatSql(@"
                INSERT INTO playerstats_groups (GroupId, GroupName, OwnerSteamId, CreatedAt)
                VALUES (@GroupId, @GroupName, @OwnerSteamId, @CreatedAt)
                ON DUPLICATE KEY UPDATE
                    GroupName = VALUES(GroupName),
                    OwnerSteamId = VALUES(OwnerSteamId)");

            string deleteInvites = FormatSql("DELETE FROM playerstats_group_invites WHERE GroupId = @GroupId");
            string insertInvite = FormatSql(@"
                INSERT INTO playerstats_group_invites (SteamId, GroupId, ExpiresAt)
                VALUES (@SteamId, @GroupId, @ExpiresAt)
                ON DUPLICATE KEY UPDATE ExpiresAt = VALUES(ExpiresAt)");

            using (MySqlConnection conn = connection)
            {
                conn.Execute(upsertGroup, group);
                conn.Execute(deleteInvites, new { group.GroupId });

                foreach (var invite in group.InvitedPlayers)
                {
                    conn.Execute(insertInvite, new { SteamId = invite.Key, GroupId = group.GroupId, ExpiresAt = invite.Value });
                }
            }
        }

        public void DeleteGroup(string groupId)
        {
            string deleteInvites = FormatSql("DELETE FROM playerstats_group_invites WHERE GroupId = @groupId");
            string clearMembers = FormatSql("UPDATE PlayerStats SET GroupId = NULL WHERE GroupId = @groupId");
            string deleteGroup = FormatSql("DELETE FROM playerstats_groups WHERE GroupId = @groupId");

            using (MySqlConnection conn = connection)
            {
                conn.Execute(deleteInvites, new { groupId });
                conn.Execute(clearMembers, new { groupId });
                conn.Execute(deleteGroup, new { groupId });
            }
        }

        public List<GroupRanking> GetGroupLeaderboard(StatsMode mode, int limit, int offset)
        {
            string orderBy = (mode == StatsMode.Both || mode == StatsMode.PVP) ? "TotalKills" : "TotalZombieKills";

            string query = FormatSql($@"
                SELECT 
                    g.GroupId,
                    g.GroupName,
                    COUNT(p.SteamId) AS MemberCount,
                    COALESCE(SUM(p.Kills), 0) AS TotalKills,
                    COALESCE(SUM(p.Zombies), 0) AS TotalZombieKills,
                    COALESCE(SUM(p.PVPDeaths + p.PVEDeaths), 0) AS TotalDeaths
                FROM playerstats_groups g
                LEFT JOIN PlayerStats p ON p.GroupId = g.GroupId
                GROUP BY g.GroupId, g.GroupName
                ORDER BY {orderBy} DESC
                LIMIT @limit OFFSET @offset");

            using (MySqlConnection conn = connection)
            {
                List<GroupRanking> rankings = conn.Query<GroupRanking>(query, new { limit, offset }).ToList();
                for (int i = 0; i < rankings.Count; i++)
                {
                    rankings[i].Rank = i + 1 + offset;
                }
                return rankings;
            }
        }

        public GroupRanking GetGroupRank(string groupId, StatsMode mode)
        {
            string orderBy = (mode == StatsMode.Both || mode == StatsMode.PVP) ? "TotalKills" : "TotalZombieKills";

            string rankQuery = FormatSql($@"
                SELECT COUNT(*) + 1
                FROM (
                    SELECT g.GroupId, COALESCE(SUM(p.{orderBy.Replace("Total", "")}), 0) AS StatTotal
                    FROM playerstats_groups g
                    LEFT JOIN PlayerStats p ON p.GroupId = g.GroupId
                    GROUP BY g.GroupId
                    HAVING StatTotal > (
                        SELECT COALESCE(SUM(p2.{orderBy.Replace("Total", "")}), 0)
                        FROM PlayerStats p2
                        WHERE p2.GroupId = @groupId
                    )
                ) AS higher");

            string statQuery = FormatSql($@"
                SELECT 
                    g.GroupId,
                    g.GroupName,
                    COUNT(p.SteamId) AS MemberCount,
                    COALESCE(SUM(p.Kills), 0) AS TotalKills,
                    COALESCE(SUM(p.Zombies), 0) AS TotalZombieKills,
                    COALESCE(SUM(p.PVPDeaths + p.PVEDeaths), 0) AS TotalDeaths
                FROM playerstats_groups g
                LEFT JOIN PlayerStats p ON p.GroupId = g.GroupId
                WHERE g.GroupId = @groupId
                GROUP BY g.GroupId, g.GroupName");

            using (MySqlConnection conn = connection)
            {
                int rank = conn.ExecuteScalar<int>(rankQuery, new { groupId });
                GroupRanking ranking = conn.QueryFirstOrDefault<GroupRanking>(statQuery, new { groupId });
                if (ranking != null)
                {
                    ranking.Rank = rank;
                }
                return ranking;
            }
        }
    }
}
