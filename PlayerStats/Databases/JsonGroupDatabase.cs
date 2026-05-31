using Newtonsoft.Json;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.Databases
{
    internal class JsonGroupDatabase : IGroupDatabase
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;

        private List<Group> groups = new();
        private readonly object lockObj = new();

        private string GroupsFilePath
        {
            get
            {
                string dir = Path.GetDirectoryName(configuration.JsonFilePath.Replace("{rocket_directory}", Directory.GetCurrentDirectory()));
                return Path.Combine(dir, "Groups.json");
            }
        }

        public void Initialize()
        {
            string path = GroupsFilePath;

            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);
                groups = JsonConvert.DeserializeObject<List<Group>>(text) ?? new List<Group>();
                Logger.Log($"Loaded {groups.Count} group(s) from Groups.json");
            }
            else
            {
                groups = new List<Group>();
                string text = JsonConvert.SerializeObject(groups, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text);
            }
        }

        public void Save()
        {
            lock (lockObj)
            {
                string path = GroupsFilePath;
                string text = JsonConvert.SerializeObject(groups, Formatting.Indented);
                File.WriteAllText(path, text);
            }
        }

        public Group GetGroup(string groupId)
        {
            lock (lockObj)
            {
                return groups.FirstOrDefault(g => g.GroupId == groupId);
            }
        }

        public Group GetGroupByMember(ulong steamId)
        {
            lock (lockObj)
            {
                return groups.FirstOrDefault(g => g.Members.Contains(steamId));
            }
        }

        public Group GetGroupByName(string name)
        {
            lock (lockObj)
            {
                return groups.FirstOrDefault(g => g.GroupName.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public List<Group> GetAllGroups()
        {
            lock (lockObj)
            {
                return groups.ToList();
            }
        }

        public void AddOrUpdateGroup(Group group)
        {
            lock (lockObj)
            {
                int index = groups.FindIndex(g => g.GroupId == group.GroupId);
                if (index >= 0)
                {
                    groups[index] = group;
                }
                else
                {
                    groups.Add(group);
                }
            }
        }

        public void DeleteGroup(string groupId)
        {
            lock (lockObj)
            {
                groups.RemoveAll(g => g.GroupId == groupId);
            }
        }

        public List<GroupRanking> GetGroupLeaderboard(StatsMode mode, int limit, int offset)
        {
            List<Group> allGroups;
            lock (lockObj)
            {
                allGroups = groups.ToList();
            }

            bool isPVP = mode == StatsMode.Both || mode == StatsMode.PVP;

            List<GroupRanking> rankings = allGroups.Select(g =>
            {
                List<PlayerStatsData> members = g.Members
                    .Select(m => pluginInstance.Database.GetPlayer(m))
                    .Where(p => p != null)
                    .ToList();

                return new GroupRanking
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    MemberCount = g.Members.Count,
                    TotalKills = members.Sum(m => m.Kills),
                    TotalZombieKills = members.Sum(m => m.Zombies),
                    TotalDeaths = members.Sum(m => m.Deaths)
                };
            }).ToList();

            if (isPVP)
            {
                rankings = rankings.OrderByDescending(r => r.TotalKills).ToList();
            }
            else
            {
                rankings = rankings.OrderByDescending(r => r.TotalZombieKills).ToList();
            }

            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].Rank = i + 1 + offset;
            }

            return rankings.Skip(offset).Take(limit).ToList();
        }

        public GroupRanking GetGroupRank(string groupId, StatsMode mode)
        {
            bool isPVP = mode == StatsMode.Both || mode == StatsMode.PVP;

            Group group = GetGroup(groupId);
            if (group == null) return null;

            List<PlayerStatsData> members = group.Members
                .Select(m => pluginInstance.Database.GetPlayer(m))
                .Where(p => p != null)
                .ToList();

            int totalKills = members.Sum(m => m.Kills);
            int totalZombieKills = members.Sum(m => m.Zombies);
            int totalDeaths = members.Sum(m => m.Deaths);

            int rank;
            lock (lockObj)
            {
                if (isPVP)
                {
                    rank = groups.Count(g =>
                    {
                        List<PlayerStatsData> gMembers = g.Members
                            .Select(m => pluginInstance.Database.GetPlayer(m))
                            .Where(p => p != null)
                            .ToList();
                        return gMembers.Sum(m => m.Kills) > totalKills;
                    }) + 1;
                }
                else
                {
                    rank = groups.Count(g =>
                    {
                        List<PlayerStatsData> gMembers = g.Members
                            .Select(m => pluginInstance.Database.GetPlayer(m))
                            .Where(p => p != null)
                            .ToList();
                        return gMembers.Sum(m => m.Zombies) > totalZombieKills;
                    }) + 1;
                }
            }

            return new GroupRanking
            {
                Rank = rank,
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                MemberCount = group.Members.Count,
                TotalKills = totalKills,
                TotalZombieKills = totalZombieKills,
                TotalDeaths = totalDeaths
            };
        }
    }
}
