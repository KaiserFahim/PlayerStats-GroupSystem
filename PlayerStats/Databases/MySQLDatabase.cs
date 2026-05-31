using Dapper;
using MySql.Data.MySqlClient;
using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.Databases
{
    internal class MySQLDatabase : IDatabase
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;
        private MySqlConnection connection => new(configuration.MySQLConnectionString);
        private string FormatSql(string query) => query.Replace("PlayerStats", configuration.PlayerStatsTableName);

        public IEnumerable<PlayerRanking> GetPlayerRankings(int amount, string orderBy = null)
        {
            if (orderBy == null)
            {
                orderBy = (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP) ? "Kills" : "Zombies";
            }
            
            string query = FormatSql($@"
                SELECT 
                    SteamId, 
                    Name, 
                    Kills, 
                    Zombies,
                    (SELECT COUNT(*) + 1 FROM PlayerStats AS p2 WHERE p2.{orderBy} > p1.{orderBy}) AS `Rank`
                FROM PlayerStats AS p1
                ORDER BY {orderBy} DESC
                LIMIT @amount");

            using (MySqlConnection conn = connection)
            {
                return conn.Query<PlayerRanking>(query, new { amount });
            }
        }

        public PlayerRanking GetPlayerRanking(ulong steamId, string orderBy = null)
        {
            if (orderBy == null)
            {
                orderBy = (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP) ? "Kills" : "Zombies";
            }

            string query = FormatSql($@"
                SELECT 
                    p1.SteamId, 
                    p1.Name, 
                    p1.Kills, 
                    p1.Zombies,
                    (SELECT COUNT(*) + 1 FROM PlayerStats AS p2 WHERE p2.{orderBy} > p1.{orderBy}) AS `Rank`
                FROM PlayerStats AS p1
                WHERE p1.SteamId = @steamId");

            using (MySqlConnection conn = connection)
            {
                return conn.QueryFirstOrDefault<PlayerRanking>(query, new { steamId });
            }
        }

        public void AddOrUpdatePlayer(PlayerStatsData player)
        {
            string query = FormatSql(@"
                INSERT INTO PlayerStats (
                    SteamId, Name, Kills, Headshots, PVPDeaths, PVEDeaths, Zombies, MegaZombies,
                    Animals, Resources, Harvests, Fish, Structures, Barricades, Playtime, UIDisabled, GroupId, LastUpdated
                ) VALUES (
                    @SteamId, @Name, @Kills, @Headshots, @PVPDeaths, @PVEDeaths, @Zombies, @MegaZombies,
                    @Animals, @Resources, @Harvests, @Fish, @Structures, @Barricades, @Playtime, @UIDisabled, @GroupId, @LastUpdated
                ) ON DUPLICATE KEY UPDATE
                    Name = @Name, Kills = @Kills, Headshots = @Headshots, PVPDeaths = @PVPDeaths,
                    PVEDeaths = @PVEDeaths, Zombies = @Zombies, MegaZombies = @MegaZombies,
                    Animals = @Animals, Resources = @Resources, Harvests = @Harvests, Fish = @Fish,
                    Structures = @Structures, Barricades = @Barricades, Playtime = @Playtime,
                    UIDisabled = @UIDisabled, GroupId = @GroupId, LastUpdated = @LastUpdated");

            player.LastUpdated = DateTime.UtcNow;

            using (MySqlConnection conn = connection)
            {
                conn.Execute(query, player);
            }
        }

        public PlayerStatsData GetOrAddPlayer(ulong steamId, string name)
        {
            PlayerStatsData player = GetPlayer(steamId);
            if (player == null)
            {
                player = new PlayerStatsData
                {
                    SteamId = steamId,
                    Name = name
                };
                AddOrUpdatePlayer(player);
            }
            return player;
        }

        public PlayerStatsData GetPlayer(ulong steamId)
        {
            string query = FormatSql("SELECT * FROM PlayerStats WHERE SteamId = @steamId");

            using (MySqlConnection conn = connection)
            {
                return conn.QueryFirstOrDefault<PlayerStatsData>(query, new { steamId });
            }
        }

        public void Initialize()
        {
            string tables = ResourceHelper.GetResourceFileContent("tables.txt");
            using (MySqlConnection conn = connection)
            {
                conn.Execute(FormatSql(tables));

                const string migrationCheck = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PlayerStats' AND COLUMN_NAME = 'PlayerID';";
                if (conn.ExecuteScalar<int>(FormatSql(migrationCheck)) > 0)
                {
                    Logger.Log($"Migrating old {configuration.PlayerStatsTableName} table...");
                    string migration = ResourceHelper.GetResourceFileContent("migration.txt");
                    conn.Execute(FormatSql(migration));
                    Logger.Log("Migration completed!");
                }
            }
        }

        public void Reload()
        {
            // No need to implement for MySQL as data is stored in the database
        }

        public void Save(IEnumerable<PlayerStatsData> playersData)
        {
            if (!playersData.Any())
            {
                return;
            }

            string query = FormatSql(@"
                INSERT INTO PlayerStats (
                    SteamId, Name, Kills, Headshots, PVPDeaths, PVEDeaths, Zombies, MegaZombies,
                    Animals, Resources, Harvests, Fish, Structures, Barricades, Playtime, UIDisabled, GroupId, LastUpdated
                ) VALUES (
                    @SteamId, @Name, @Kills, @Headshots, @PVPDeaths, @PVEDeaths, @Zombies, @MegaZombies,
                    @Animals, @Resources, @Harvests, @Fish, @Structures, @Barricades, @Playtime, @UIDisabled, @GroupId, @LastUpdated
                ) ON DUPLICATE KEY UPDATE
                    Name = VALUES(Name),
                    Kills = VALUES(Kills),
                    Headshots = VALUES(Headshots),
                    PVPDeaths = VALUES(PVPDeaths),
                    PVEDeaths = VALUES(PVEDeaths),
                    Zombies = VALUES(Zombies),
                    MegaZombies = VALUES(MegaZombies),
                    Animals = VALUES(Animals),
                    Resources = VALUES(Resources),
                    Harvests = VALUES(Harvests),
                    Fish = VALUES(Fish),
                    Structures = VALUES(Structures),
                    Barricades = VALUES(Barricades),
                    Playtime = VALUES(Playtime),
                    UIDisabled = VALUES(UIDisabled),
                    GroupId = VALUES(GroupId),
                    LastUpdated = VALUES(LastUpdated)");

            DateTime now = DateTime.UtcNow;
            IEnumerable<PlayerStatsData> updatedPlayersData = playersData.Select(p =>
            {
                p.LastUpdated = now;
                return p;
            });

            using (MySqlConnection conn = connection)
            {
                conn.Execute(query, updatedPlayersData);
            }
        }
    }
}