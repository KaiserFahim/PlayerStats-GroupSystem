using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace RestoreMonarchy.PlayerStats.Components
{
    public partial class PlayerStatsComponent : MonoBehaviour
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;

        public Player Player { get; private set; }
        public string Name => Player.channel.owner.playerID.characterName;
        public CSteamID SteamID => Player.channel.owner.playerID.steamID;
        public ulong SteamId => Player.channel.owner.playerID.steamID.m_SteamID;
        public PlayerStatsData PlayerData { get; private set; }
        public PlayerStatsData SessionPlayerData { get; private set; }
        public JoinSnapshot groupJoinSnapshot;
        public bool Loaded { get; private set; }

        private Reward GetCurrentReward() 
        { 
            if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
            {
                return configuration.Rewards.OrderByDescending(x => x.Treshold).FirstOrDefault(x => x.Treshold <= PlayerData.Kills);
            } else
            {
                return configuration.Rewards.OrderByDescending(x => x.Treshold).FirstOrDefault(x => x.Treshold <= PlayerData.Zombies);
            }
        }

        private Reward GetNextReward()
        {
            if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
            {
                return configuration.Rewards.OrderBy(x => x.Treshold).FirstOrDefault(x => x.Treshold > PlayerData.Kills);
            }
            else
            {
                return configuration.Rewards.OrderBy(x => x.Treshold).FirstOrDefault(x => x.Treshold > PlayerData.Zombies);
            }
        }

        void Awake()
        {
            Player = gameObject.GetComponent<Player>();
            PlayerData = new() { SteamId = SteamId, Name = Name }; // set to avoid null reference exception
            SessionPlayerData = new()
            {
                SteamId = SteamId,
                Name = Name
            };

            ThreadHelper.RunAsynchronously(() =>
            {
                PlayerStatsData playerData;
                try
                {
                    playerData = pluginInstance.Database.GetOrAddPlayer(SteamId, Name);
                    Loaded = true;
                }
                catch (Exception ex)
                {
                    ThreadHelper.RunSynchronously(() =>
                    {
                        Logger.Log($"Failed to load player data for {Name} ({SteamId}): {ex.Message}");
                    });
                    playerData = PlayerData;
                }

                ThreadHelper.RunSynchronously(() =>
                {
                    if (Player != null && this != null)
                    {
                        playerData.Name = Name;
                        PlayerData = playerData;

                        PlayerData.Kills += SessionPlayerData.Kills;
                        PlayerData.Headshots += SessionPlayerData.Headshots;
                        PlayerData.PVPDeaths += SessionPlayerData.PVPDeaths;
                        PlayerData.PVEDeaths += SessionPlayerData.PVEDeaths;
                        PlayerData.Zombies += SessionPlayerData.Zombies;
                        PlayerData.MegaZombies += SessionPlayerData.MegaZombies;
                        PlayerData.Animals += SessionPlayerData.Animals;
                        PlayerData.Resources += SessionPlayerData.Resources;
                        PlayerData.Harvests += SessionPlayerData.Harvests;
                        PlayerData.Fish += SessionPlayerData.Fish;
                        PlayerData.Structures += SessionPlayerData.Structures;
                        PlayerData.Barricades += SessionPlayerData.Barricades;
                        PlayerData.Playtime += SessionPlayerData.Playtime;

                        bool enabled = !playerData.UIDisabled ?? configuration.ShowUIEffectByDefault;
                        if (configuration.EnableUIEffect && enabled)
                        {
                            SendUIEffect();
                        }
                    }

                    // Give rewards if he doesn't have them
                    UnturnedPlayer unturnedPlayer = UnturnedPlayer.FromPlayer(Player);
                    List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(unturnedPlayer, true);
                    foreach (Reward reward in configuration.Rewards)
                    {
                        if (groups.Exists(x => x.Id.Equals(reward.PermissionGroup, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                        {
                            if (reward.Treshold <= PlayerData.Kills)
                            {
                                R.Permissions.AddPlayerToGroup(reward.PermissionGroup, unturnedPlayer);
                            }
                        }
                        else
                        {
                            if (reward.Treshold <= PlayerData.Zombies)
                            {
                                R.Permissions.AddPlayerToGroup(reward.PermissionGroup, unturnedPlayer);
                            }
                        }
                    }
                });
            });
        }

        void Start()
        {
            InvokeRepeating(nameof(UpdatePlaytime), 1, 1);
            InvokeRepeating(nameof(UpdateUIRanking), 90, 90);
        }

        void OnDestroy()
        {
            CancelInvoke(nameof(UpdatePlaytime));
            CancelInvoke(nameof(UpdateUIRanking));

            CloseUIEffect();
        }

        ulong prePlaytime = 0;
        private void UpdatePlaytime()
        {
            if (PlayerData == null)
            {
                prePlaytime++;
                return;
            }
            
            if (prePlaytime > 0)
            {
                PlayerData.Playtime += prePlaytime;
                SessionPlayerData.Playtime += prePlaytime;
                prePlaytime = 0;
            }

            PlayerData.Playtime++;
            SessionPlayerData.Playtime++;
        }

        private void UpdateUIRanking()
        {
            if (!isOpen)
            {
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                PlayerRanking playerRanking = pluginInstance.Database.GetPlayerRanking(SteamId);
                ThreadHelper.RunSynchronously(() =>
                {
                    UpdateUIEffectRank(playerRanking);
                });
            });
        }

        internal void OnPlayerDeath(Player killer, ELimb limb, EDeathCause cause)
        {
            if (killer != null && killer != Player)
            {
                PlayerData.PVPDeaths++;
                SessionPlayerData.PVPDeaths++;

                PlayerStatsComponent killerComponent = killer.GetComponent<PlayerStatsComponent>();
                if (killerComponent != null)
                {
                    killerComponent.PlayerData.Kills++;
                    killerComponent.SessionPlayerData.Kills++;
                    if (limb == ELimb.SKULL)
                    {
                        killerComponent.PlayerData.Headshots++;
                        killerComponent.SessionPlayerData.Headshots++;
                    }
                    killerComponent.UpdateUIEffect();
                    killerComponent.CheckGiveReward();

                    killerComponent.CheckAutomaticBan();
                }
                UpdateUIEffect();
            } else
            {
                PlayerData.PVEDeaths++;
                SessionPlayerData.PVEDeaths++;
                if (configuration.ActualStatsMode == StatsMode.PVE || configuration.ShowCombinedDeaths)
                {
                    UpdateUIEffect();
                }
            }

            CheckAutomaticBan();
            RefreshGroupUIMembers();
        }

        private void RefreshGroupUIMembers()
        {
            if (string.IsNullOrEmpty(PlayerData.GroupId))
                return;

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(PlayerData.GroupId);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null) return;
                    foreach (ulong memberId in group.Members)
                    {
                        foreach (Player p in PlayerTool.EnumeratePlayers())
                        {
                            if (p.channel.owner.playerID.steamID.m_SteamID == memberId)
                            {
                                PlayerStatsComponent comp = p.GetComponent<PlayerStatsComponent>();
                                if (comp != null && comp.isGroupPanelOpen)
                                {
                                    comp.UpdateUIGroupPanelInfo();
                                }
                                break;
                            }
                        }
                    }
                });
            });
        }

        internal void CheckGiveReward()
        {
            if (!configuration.EnableRewards)
            {
                return;
            }

            Reward reward = GetCurrentReward();
            if (reward != null)
            {
                UnturnedPlayer unturnedPlayer = UnturnedPlayer.FromPlayer(Player);
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(unturnedPlayer, false);
                if (!groups.Exists(x => x.Id.Equals(reward.PermissionGroup, StringComparison.OrdinalIgnoreCase)))
                {
                    RocketPermissionsProviderResult result = R.Permissions.AddPlayerToGroup(reward.PermissionGroup, unturnedPlayer);
                    if (result != RocketPermissionsProviderResult.Success)
                    {
                        Logger.Log($"Warning >> Failed to give {reward.PermissionGroup} permission group as reward to {unturnedPlayer.DisplayName}. Reason: {result}", ConsoleColor.Yellow);
                        return;
                    }

                    string treshold = reward.Treshold.ToString("N0");
                    if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                    {
                        pluginInstance.SendMessageToPlayer(unturnedPlayer, "RewardReceivedPVP", reward.Name, treshold);
                    } else
                    {
                        pluginInstance.SendMessageToPlayer(unturnedPlayer, "RewardReceivedPVE", reward.Name, treshold);
                    }
                }                
            }
        }

        internal void OnPlayerUpdatedStat(EPlayerStat stat)
        {
            switch (stat)
            {
                case EPlayerStat.KILLS_ANIMALS:
                    PlayerData.Animals++;
                    SessionPlayerData.Animals++;
                    break;
                case EPlayerStat.KILLS_ZOMBIES_NORMAL:
                    PlayerData.Zombies++;
                    SessionPlayerData.Zombies++;
                    CheckGiveReward();
                    break;
                case EPlayerStat.KILLS_ZOMBIES_MEGA:
                    PlayerData.MegaZombies++;
                    SessionPlayerData.MegaZombies++;
                    break;
                case EPlayerStat.FOUND_RESOURCES:
                    PlayerData.Resources++;
                    SessionPlayerData.Resources++;
                    break;
                case EPlayerStat.FOUND_FISHES:
                    PlayerData.Fish++;
                    SessionPlayerData.Fish++;
                    break;
                case EPlayerStat.FOUND_PLANTS:
                    PlayerData.Harvests++;
                    SessionPlayerData.Harvests++;
                    break;
            }

            if (configuration.ActualStatsMode == StatsMode.PVE)
            {
                UpdateUIEffect();
            }
        }

        internal void OnStructureSpawned()
        {
            PlayerData.Structures++;
            SessionPlayerData.Structures++;
        }

        internal void OnBarricadeSpawned()
        {
            PlayerData.Barricades++;
            SessionPlayerData.Barricades++;
        }
    }
}
