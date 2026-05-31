using RestoreMonarchy.PlayerStats.Models;
using System.Collections.Generic;

namespace RestoreMonarchy.PlayerStats.Databases
{
    public interface IGroupDatabase
    {
        void Initialize();
        void Save();
        Group GetGroup(string groupId);
        Group GetGroupByMember(ulong steamId);
        Group GetGroupByName(string name);
        List<Group> GetAllGroups();
        void AddOrUpdateGroup(Group group);
        void DeleteGroup(string groupId);
        List<GroupRanking> GetGroupLeaderboard(StatsMode mode, int limit, int offset);
        GroupRanking GetGroupRank(string groupId, StatsMode mode);
    }
}
