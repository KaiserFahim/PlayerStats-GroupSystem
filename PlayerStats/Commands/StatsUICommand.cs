using RestoreMonarchy.PlayerStats.Components;
using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class StatsUICommand : IRocketCommand
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component == null)
            {
                pluginInstance.SendMessageToPlayer(player, "PlayerStatsNotLoaded");
                return;
            }

            if (!pluginInstance.Configuration.Instance.EnableUIEffect)
            {
                pluginInstance.SendMessageToPlayer(player, "StatsUIEffectDisabled");
                return;
            }

            bool currentUIState = component.PlayerData.UIDisabled ?? !pluginInstance.Configuration.Instance.ShowUIEffectByDefault;
            component.PlayerData.UIDisabled = !currentUIState;

            if (component.PlayerData.UIDisabled.Value)
            {
                component.CloseUIEffect();
                pluginInstance.SendMessageToPlayer(player, "StatsUIDisabled");
            }
            else
            {
                component.SendUIEffect();
                pluginInstance.SendMessageToPlayer(player, "StatsUIEnabled");
            }
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "statsui";

        public string Help => "";

        public string Syntax => "";

        public List<string> Aliases => new();

        public List<string> Permissions => new();
    }
}
