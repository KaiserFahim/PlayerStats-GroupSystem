using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.Core.Logging;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.Components
{
    public partial class PlayerStatsComponent
    {
        private bool isOpen = false;
        private bool isGroupPanelOpen = false;
        private const short Key = 22512;
        private ITransportConnection TransportConnection => Player.channel.GetOwnerTransportConnection();

        public void SendUIEffect()
        {
            if (isOpen)
            {
                Logger.Log($"PlayerStats UI is already open for {Name} ({SteamId})");
                return;
            }

            isOpen = true;
            EffectManager.sendUIEffect(configuration.UIEffectId, Key, TransportConnection, true);

            if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
            {
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Text", pluginInstance.Translate("UI_Kills"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Text", pluginInstance.Translate("UI_Deaths"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Text", pluginInstance.Translate("UI_Headshots"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Text", pluginInstance.Translate("UI_Accuracy"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Text", pluginInstance.Translate("UI_KDR"));
            }
            else
            {
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Text", pluginInstance.Translate("UI_ZombieKills"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Text", pluginInstance.Translate("UI_MegaZombieKills"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Text", pluginInstance.Translate("UI_AnimalKills"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Text", pluginInstance.Translate("UI_ResourcesGathered"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Text", pluginInstance.Translate("UI_PVEDeaths"));
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Text", pluginInstance.Translate("UI_Rank"));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Footer_Text", pluginInstance.Translate("UI_Footer"));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Stats_Text", "-");

            if (configuration.EnableRewards)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStats_ProgressBar", true);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStats_ProgressBar", false);
            }

            UpdateUIEffect();
            UpdateUIRanking();
            UpdateUIGroup();
            ShowUIEffect();
        }

        private int prevPercentageProgress = 0;

        public void UpdateUIEffect()
        {
            if (!isOpen) return;

            if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
            {
                string kills = PlayerData.Kills.ToString("N0");
                string deaths = PlayerData.Deaths.ToString("N0");
                string headshots = PlayerData.Headshots.ToString("N0");
                string hsPercentage = (PlayerData.Kills == 0 ? "0" : (((decimal)PlayerData.Headshots / PlayerData.Kills) * 100).ToString("N0")) + "%";
                string kdr = PlayerData.Deaths == 0 ? PlayerData.Kills.ToString("N2") : ((decimal)PlayerData.Kills / PlayerData.Deaths).ToString("N2");

                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Stats_Text", kills);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Stats_Text", deaths);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Stats_Text", headshots);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Stats_Text", hsPercentage);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Stats_Text", kdr);
            }
            else
            {
                string zombieKills = PlayerData.Zombies.ToString("N0");
                string megaZombieKills = PlayerData.MegaZombies.ToString("N0");
                string animalKills = PlayerData.Animals.ToString("N0");
                string resourcesGathered = PlayerData.Resources.ToString("N0");
                string pveDeaths = PlayerData.Deaths.ToString("N0");

                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Stats_Text", zombieKills);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Stats_Text", megaZombieKills);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Stats_Text", animalKills);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Stats_Text", resourcesGathered);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Stats_Text", pveDeaths);
            }

            if (configuration.EnableRewards)
            {
                Reward reward = GetNextReward();
                if (reward != null)
                {
                    string nextReward, progress;
                    int progressPercentage;
                    if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                    {
                        nextReward = pluginInstance.Translate("UI_NextReward", reward.Name);
                        progress = pluginInstance.Translate("UI_RewardProgress", PlayerData.Kills.ToString("N0"), reward.Treshold.ToString("N0"));
                        progressPercentage = (int)((PlayerData.Kills / (float)reward.Treshold) * 100);
                    }
                    else
                    {
                        nextReward = pluginInstance.Translate("UI_NextReward", reward.Name);
                        progress = pluginInstance.Translate("UI_RewardProgressPVE", PlayerData.Zombies.ToString("N0"), reward.Treshold.ToString("N0"));
                        progressPercentage = (int)((PlayerData.Zombies / (float)reward.Treshold) * 100);
                    }
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Reward_Text", nextReward);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Progress_Text", progress);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"PlayerStats_ProgressBar_Fill_{prevPercentageProgress}", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"PlayerStats_ProgressBar_Fill_{progressPercentage}", true);
                    prevPercentageProgress = progressPercentage;
                }
                else
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStats_ProgressBar", false);
                }
            }
        }

        public void UpdateUIEffectRank(PlayerRanking playerRanking)
        {
            if (!isOpen) return;
            string rankString = playerRanking.IsUnranked() ? "-" : "#" + playerRanking.Rank.ToString("N0");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Stats_Text", rankString);
        }

        public void ShowUIEffect()
        {
            if (!isOpen) return;
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStatsUIHolder", true);
        }

        public void HideUIEffect()
        {
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStatsUIHolder", false);
        }

        public void CloseUIEffect()
        {
            EffectManager.askEffectClearByID(configuration.UIEffectId, TransportConnection);
            isOpen = false;
            isGroupPanelOpen = false;
        }

        public void ToggleUIGroupPanel()
        {
            if (isGroupPanelOpen) HideUIGroupPanel();
            else SendUIGroupPanel();
        }

        public void SendUIGroupPanel()
        {
            if (isGroupPanelOpen) return;
            if (isOpen) HideUIEffect();
            isGroupPanelOpen = true;
            EffectManager.sendUIEffect(configuration.UIEffectId, Key, TransportConnection, true);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStats_ProgressBar", false);
            UpdateUIGroupPanelInfo();
            ShowUIEffect();
        }

        public void UpdateUIGroupPanelInfo()
        {
            if (!isGroupPanelOpen) return;

            if (string.IsNullOrEmpty(PlayerData.GroupId))
            {
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Text", pluginInstance.Translate("UI_GroupPanel_NoGroup"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Stats_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Stats_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Stats_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Stats_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Stats_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Stats_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Group_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Reward_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Progress_Text", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Footer_Text", pluginInstance.Translate("UI_GroupPanelFooter"));
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                try
                {
                    Group group = pluginInstance.GroupDatabase.GetGroup(PlayerData.GroupId);
                    if (group == null) return;

                    int totalKills = 0, totalDeaths = 0;

                    foreach (ulong memberId in group.Members)
                    {
                        JoinSnapshot snap = group.JoinSnapshots.ContainsKey(memberId) ? group.JoinSnapshots[memberId] : new JoinSnapshot();

                        PlayerStatsComponent memberComp = null;
                        foreach (Player p in PlayerTool.EnumeratePlayers())
                        {
                            if (p.channel.owner.playerID.steamID.m_SteamID == memberId)
                            {
                                memberComp = p.GetComponent<PlayerStatsComponent>();
                                break;
                            }
                        }

                        if (memberComp != null)
                        {
                            totalKills += Math.Max(0, memberComp.PlayerData.Kills - snap.Kills);
                            totalDeaths += Math.Max(0, memberComp.PlayerData.PVPDeaths - snap.PVPDeaths);
                            totalDeaths += Math.Max(0, memberComp.PlayerData.PVEDeaths - snap.PVEDeaths);
                        }
                        else
                        {
                            PlayerStatsData dbData = pluginInstance.Database.GetPlayer(memberId);
                            if (dbData != null)
                            {
                                totalKills += Math.Max(0, dbData.Kills - snap.Kills);
                                totalDeaths += Math.Max(0, dbData.PVPDeaths - snap.PVPDeaths);
                                totalDeaths += Math.Max(0, dbData.PVEDeaths - snap.PVEDeaths);
                            }
                        }
                    }

                    double totalKDR = totalDeaths == 0 ? totalKills : (double)totalKills / totalDeaths;

                    ThreadHelper.RunSynchronously(() =>
                    {
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Text", pluginInstance.Translate("UI_GroupPanel_Tag", group.GroupName));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Rank_Stats_Text", pluginInstance.Translate("UI_GroupPanel_Members", group.Members.Count));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Text", pluginInstance.Translate("UI_Kills"));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Kills_Stats_Text", totalKills.ToString("N0"));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Text", pluginInstance.Translate("UI_Deaths"));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Deaths_Stats_Text", totalDeaths.ToString("N0"));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Text", pluginInstance.Translate("UI_KDR"));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_KD_Stats_Text", totalKDR.ToString("N2"));
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Text", "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_HeadShot_Stats_Text", "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Text", "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Stats_Accuracy_Stats_Text", "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Footer_Text", pluginInstance.Translate("UI_GroupPanelFooter"));
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "PlayerStats_ProgressBar", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Group_Text", "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Reward_Text", "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerStats_Progress_Text", "");
                    });
                }
                catch (Exception ex)
                {
                    ThreadHelper.RunSynchronously(() => Logger.Log($"Group UI error for {Name}: {ex.Message}"));
                }
            });
        }

        public void HideUIGroupPanel()
        {
            EffectManager.askEffectClearByID(configuration.UIEffectId, TransportConnection);
            isGroupPanelOpen = false;
            if (isOpen)
            {
                isOpen = false;
                SendUIEffect();
            }
        }
    }
}
