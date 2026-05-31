using RestoreMonarchy.PlayerStats.Components;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class SessionStatsCommand : IRocketCommand
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1 && caller is ConsolePlayer)
            {
                pluginInstance.SendMessageToPlayer(caller, "SessionStatsCommandSyntax");
                return;
            }

            UnturnedPlayer player = command.Length > 0 ? UnturnedPlayer.FromName(command[0]) : (UnturnedPlayer)caller;
            if (player == null)
            {
                pluginInstance.SendMessageToPlayer(caller, "PlayerNotFound", command[0]);
                return;
            }

            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component == null || component.SessionPlayerData == null)
            {
                pluginInstance.SendMessageToPlayer(caller, "PlayerStatsNotLoaded", player.DisplayName);
                return;
            }

            PlayerStatsData playerData = component.SessionPlayerData;
            if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
            {
                string kills = playerData.Kills.ToString("N0");
                string deaths = playerData.Deaths.ToString("N0");
                string kdr = deaths == "0" ? kills : (playerData.Kills / playerData.Deaths).ToString("N2");
                string hsPercentage = (playerData.Kills == 0 ? "0" : (((decimal)playerData.Headshots / playerData.Kills) * 100).ToString("N0")) + "%";

                if (caller.Id == playerData.SteamId.ToString())
                {
                    pluginInstance.SendMessageToPlayer(caller, "YourPVPSessionStats", kills, deaths, kdr, hsPercentage);
                }
                else
                {
                    pluginInstance.SendMessageToPlayer(caller, "OtherPVPSessionStats", playerData.Name, kills, deaths, kdr, hsPercentage);
                }
            }

            if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVE)
            {
                string zombies = playerData.Zombies.ToString("N0");
                string megaZombies = playerData.MegaZombies.ToString("N0");
                string animals = playerData.Animals.ToString("N0");
                string resources = playerData.Resources.ToString("N0");
                string harvests = playerData.Harvests.ToString("N0");
                string fish = playerData.Fish.ToString("N0");

                if (caller.Id == playerData.SteamId.ToString())
                {
                    pluginInstance.SendMessageToPlayer(caller, "YourPVESessionStats", zombies, megaZombies, animals, resources, harvests, fish);
                }
                else
                {
                    pluginInstance.SendMessageToPlayer(caller, "OtherPVESessionStats", playerData.Name, zombies, megaZombies, animals, resources, harvests, fish);
                }
            }
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "sessionstats";

        public string Help => "";

        public string Syntax => "";

        public List<string> Aliases => new() { "sstats" };

        public List<string> Permissions => new();
    }
}
