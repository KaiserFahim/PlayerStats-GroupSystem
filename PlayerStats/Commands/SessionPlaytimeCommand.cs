using RestoreMonarchy.PlayerStats.Components;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class SessionPlaytimeCommand : IRocketCommand
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1 && caller is ConsolePlayer)
            {
                pluginInstance.SendMessageToPlayer(caller, "SessionPlaytimeCommandSyntax");
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

            TimeSpan timespan = TimeSpan.FromSeconds(playerData.Playtime);
            string playtime = pluginInstance.FormatTimespan(timespan);
            if (playerData.SteamId.ToString() == caller.Id)
            {
                pluginInstance.SendMessageToPlayer(caller, "YourSessionPlaytime", playtime);
            }
            else
            {
                pluginInstance.SendMessageToPlayer(caller, "OtherSessionPlaytime", playerData.Name, playtime);
            }
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "sessionplaytime";

        public string Help => "";

        public string Syntax => "";

        public List<string> Aliases => new() { "splaytime" };

        public List<string> Permissions => new();
    }
}
