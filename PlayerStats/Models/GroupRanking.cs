namespace RestoreMonarchy.PlayerStats.Models
{
    public class GroupRanking
    {
        public int Rank { get; set; }
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public int MemberCount { get; set; }
        public int TotalKills { get; set; }
        public int TotalZombieKills { get; set; }
        public int TotalDeaths { get; set; }
    }
}
