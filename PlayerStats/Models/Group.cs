using System;
using System.Collections.Generic;

namespace RestoreMonarchy.PlayerStats.Models
{
    public class Group
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public ulong OwnerSteamId { get; set; }
        public List<ulong> Members { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<ulong, DateTime> InvitedPlayers { get; set; }
        public Dictionary<ulong, JoinSnapshot> JoinSnapshots { get; set; }

        public Group()
        {
            Members = new List<ulong>();
            InvitedPlayers = new Dictionary<ulong, DateTime>();
            JoinSnapshots = new Dictionary<ulong, JoinSnapshot>();
        }
    }
}
