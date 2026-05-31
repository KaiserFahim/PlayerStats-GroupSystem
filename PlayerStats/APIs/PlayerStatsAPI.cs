using RestoreMonarchy.PlayerStats.Components;
using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.APIs
{
    public static class PlayerStatsAPI
    {
        private static PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;

        public static void GetPlayerRanking(ulong steamId, bool pvp, Action<PlayerRanking> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                string orderBy = pvp ? "Kills" : "Zombies";
                PlayerRanking playerRanking = pluginInstance.Database.GetPlayerRanking(steamId, orderBy);

                ThreadHelper.RunSynchronously(() =>
                {
                    callback(playerRanking);
                });
            });
        }

        public static void GetPlayerRankings(int limit, bool pvp, Action<IEnumerable<PlayerRanking>> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                string orderBy = pvp ? "Kills" : "Zombies";
                IEnumerable<PlayerRanking> playerRankings = pluginInstance.Database.GetPlayerRankings(limit, orderBy);

                ThreadHelper.RunSynchronously(() =>
                {
                    callback(playerRankings);
                });
            });
        }

        public static void GetPlayerStats(ulong steamId, Action<PlayerStatsData> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                PlayerStatsData playerStats = pluginInstance.Database.GetPlayer(steamId);

                ThreadHelper.RunSynchronously(() =>
                {
                    callback(playerStats);
                });
            });
        }

        public static PlayerStatsData GetPlayerStats(Player player)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component == null)
            {
                throw new Exception("PlayerStats component is not attached to the player!");
            }

            return component.PlayerData;
        }

        public static bool IsPlayerInGroup(ulong steamId)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            PlayerStatsData data = pluginInstance.Database.GetPlayer(steamId);
            return data != null && !string.IsNullOrEmpty(data.GroupId);
        }

        public static string GetPlayerGroupId(ulong steamId)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            PlayerStatsData data = pluginInstance.Database.GetPlayer(steamId);
            return data?.GroupId;
        }

        public static void GetPlayerGroup(ulong steamId, Action<Group> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroupByMember(steamId);
                ThreadHelper.RunSynchronously(() => callback(group));
            });
        }

        public static void GetGroupById(string groupId, Action<Group> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(groupId);
                ThreadHelper.RunSynchronously(() => callback(group));
            });
        }

        public static void GetGroupByName(string groupName, Action<Group> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroupByName(groupName);
                ThreadHelper.RunSynchronously(() => callback(group));
            });
        }

        public static void GetGroupLeaderboard(bool pvp, int limit, int offset, Action<List<GroupRanking>> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            StatsMode mode = pvp ? StatsMode.PVP : StatsMode.PVE;
            ThreadHelper.RunAsynchronously(() =>
            {
                List<GroupRanking> rankings = pluginInstance.GroupDatabase.GetGroupLeaderboard(mode, limit, offset);
                ThreadHelper.RunSynchronously(() => callback(rankings));
            });
        }

        public static void GetGroupRank(string groupId, bool pvp, Action<GroupRanking> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            StatsMode mode = pvp ? StatsMode.PVP : StatsMode.PVE;
            ThreadHelper.RunAsynchronously(() =>
            {
                GroupRanking ranking = pluginInstance.GroupDatabase.GetGroupRank(groupId, mode);
                ThreadHelper.RunSynchronously(() => callback(ranking));
            });
        }

        public static void GetGroupMembers(string groupId, Action<List<PlayerStatsData>> callback)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(groupId);
                List<PlayerStatsData> members = new();
                if (group != null)
                {
                    foreach (ulong memberId in group.Members)
                    {
                        PlayerStatsData memberData = pluginInstance.Database.GetPlayer(memberId);
                        if (memberData != null)
                        {
                            members.Add(memberData);
                        }
                    }
                }

                ThreadHelper.RunSynchronously(() => callback(members));
            });
        }

        public static int GetGroupMemberCount(string groupId)
        {
            if (pluginInstance == null)
            {
                throw new Exception("PlayerStats plugin is not loaded!");
            }

            Group group = pluginInstance.GroupDatabase.GetGroup(groupId);
            return group?.Members.Count ?? 0;
        }
    }
}
