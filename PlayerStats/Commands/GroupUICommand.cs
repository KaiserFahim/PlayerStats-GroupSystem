using RestoreMonarchy.PlayerStats.Components;
using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class GroupUICommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "groupui";

        public string Help => "Toggle group UI panel";

        public string Syntax => "";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();
            if (component != null)
            {
                component.ToggleUIGroupPanel();
            }
        }
    }
}
