using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using SDG.Unturned;

namespace RestoreMonarchy.PlayerStats.Components
{
    public partial class PlayerStatsComponent
    {
        private void UpdateUIGroup()
        {
            if (!isOpen)
            {
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = null;
                GroupRanking ranking = null;

                if (!string.IsNullOrEmpty(PlayerData.GroupId))
                {
                    group = pluginInstance.GroupDatabase.GetGroup(PlayerData.GroupId);
                    if (group != null)
                    {
                        ranking = pluginInstance.GroupDatabase.GetGroupRank(group.GroupId, configuration.ActualStatsMode);
                    }
                }

                ThreadHelper.RunSynchronously(() =>
                {
                    string groupText;
                    if (group != null)
                    {
                        string rankStr = ranking != null ? "#" + ranking.Rank.ToString("N0") : "-";
                        groupText = pluginInstance.Translate("UI_GroupText", group.GroupName, rankStr);
                    }
                    else
                    {
                        groupText = pluginInstance.Translate("UI_NoGroupText");
                    }

                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Group_Text", groupText);
                });
            });
        }
    }
}
