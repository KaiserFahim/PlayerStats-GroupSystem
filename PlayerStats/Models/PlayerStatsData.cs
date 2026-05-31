using Newtonsoft.Json;
using Rocket.Core.Logging;
using System;

namespace RestoreMonarchy.PlayerStats.Models
{
    public class PlayerStatsData
    {
        public ulong SteamId { get; set; }
        public string Name { get; set; }
        public int Kills { get; set; }
        public int Headshots { get; set; }
        public int PVPDeaths { get; set; }
        public int PVEDeaths { get; set; }
        public int Zombies { get; set; }
        public int MegaZombies { get; set; }
        public int Animals { get; set; }
        public int Resources { get; set; }
        public int Harvests { get; set; }
        public int Fish { get; set; }
        public int Structures { get; set; }
        public int Barricades { get; set; }
        public ulong Playtime  { get; set; }
        public bool? UIDisabled { get; set; }
        public string GroupId { get; set; }
        public DateTime LastUpdated { get; set; }

        [JsonIgnore]
        public int Deaths
        {
            get
            {
                if (PlayerStatsPlugin.Instance.Configuration.Instance.ShowCombinedDeaths)
                {
                    return PVPDeaths + PVEDeaths;
                }
                else
                {
                    return PlayerStatsPlugin.Instance.Configuration.Instance.ActualStatsMode == StatsMode.Both || PlayerStatsPlugin.Instance.Configuration.Instance.ActualStatsMode == StatsMode.PVP ? PVPDeaths : PVEDeaths;
                }
            }
        }

        public long GetStatValue(string name)
        {
            switch (name?.ToLower())
            {
                case "kills":
                    return Kills;
                case "headshots":
                    return Headshots;
                case "pvpdeaths":
                    return PVPDeaths;
                case "pvedeaths":
                    return PVEDeaths;
                case "zombies":
                    return Zombies;
                case "megazombies":
                    return MegaZombies;
                case "animals":
                    return Animals;
                case "resources":
                    return Resources;
                case "harvests":
                    return Harvests;
                case "fish":
                    return Fish;
                case "structures":
                    return Structures;
                case "barricades":
                    return Barricades;
                case "playtime":
                    return (long)Playtime; // Assuming Playtime is in seconds
                case "accuracy":
                    return Kills > 0 ? (long)((Headshots / (double)Kills) * 100) : 0; // Calculate accuracy as percentage of headshots to kills
                case "deaths":
                    return Deaths; // Returns combined deaths if configured
                default:
                    return 0;
            }
        }
    }
}
