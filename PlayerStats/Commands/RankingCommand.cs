using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.API;
using System.Collections.Generic;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class RankingCommand : IRocketCommand
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;

        public void Execute(IRocketPlayer caller, string[] command)
        {
            int amount = 5;
            if (command.Length > 0 && caller is ConsolePlayer)
            {
                int.TryParse(command[0], out amount);
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                List<PlayerRanking> playerRankings = pluginInstance.Database.GetPlayerRankings(amount).Where(x => !x.IsUnranked()).ToList();
                amount = playerRankings.Count;
                ThreadHelper.RunSynchronously(() =>
                {
                    if (playerRankings.Count == 0)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "NoRankingPlayersFound");
                        return;
                    }

                    if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "RankingListHeaderPVP", amount);
                        foreach (PlayerRanking playerRanking in playerRankings)
                        {
                            string rank = playerRanking.Rank.ToString();
                            string name = playerRanking.Name;
                            string kills = playerRanking.Kills.ToString("N0");
                            pluginInstance.SendMessageToPlayer(caller, "RankingListItemPVP", rank, name, kills);
                        }
                    }
                    else
                    {
                        pluginInstance.SendMessageToPlayer(caller, "RankingListHeaderPVE", amount);
                        foreach (PlayerRanking playerRanking in playerRankings)
                        {
                            string rank = playerRanking.Rank.ToString();
                            string name = playerRanking.Name;
                            string zombies = playerRanking.Zombies.ToString("N0");
                            pluginInstance.SendMessageToPlayer(caller, "RankingListItemPVE", rank, name, zombies);
                        }
                    }
                });
            });
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "ranking";

        public string Help => "";

        public string Syntax => "";

        public List<string> Aliases => new();

        public List<string> Permissions => new();
    }
}
