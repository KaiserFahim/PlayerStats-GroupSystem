using Rocket.API;
using System.Collections.Generic;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class GroupCommandsCommand : IRocketCommand
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "groupcommands";

        public string Help => "Show all group management commands";

        public string Syntax => "";

        public List<string> Aliases => new() { "gcmds" };

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            pluginInstance.SendMessageToPlayer(caller, "GroupCommandsHeader");
            pluginInstance.SendMessageToPlayer(caller, "GroupCommandsNoGroup");
            pluginInstance.SendMessageToPlayer(caller, "GroupCommandsJoined");
            pluginInstance.SendMessageToPlayer(caller, "GroupCommandsOwner");
            pluginInstance.SendMessageToPlayer(caller, "GroupCommandsFooter");
        }
    }
}
