using RestoreMonarchy.PlayerStats.Components;
using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestoreMonarchy.PlayerStats.Commands
{
    public class GroupCommand : IRocketCommand
    {
        private PlayerStatsPlugin pluginInstance => PlayerStatsPlugin.Instance;
        private PlayerStatsConfiguration configuration => pluginInstance.Configuration.Instance;

        private static readonly Dictionary<ulong, DateTime> disbandConfirmations = new();

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "group";

        public string Help => "Group management commands";

        public string Syntax => "<create|invite|join|leave|info|leaderboard|disband|kick|transfer|stats>";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupCommandsHeader");
                pluginInstance.SendMessageToPlayer(caller, "GroupCommandsNoGroup");
                pluginInstance.SendMessageToPlayer(caller, "GroupCommandsJoined");
                pluginInstance.SendMessageToPlayer(caller, "GroupCommandsOwner");
                pluginInstance.SendMessageToPlayer(caller, "GroupCommandsFooter");
                return;
            }

            string subCommand = command[0].ToLower();
            string[] args = command.Skip(1).ToArray();

            switch (subCommand)
            {
                case "create":
                    HandleCreate(caller, args);
                    break;
                case "invite":
                    HandleInvite(caller, args);
                    break;
                case "join":
                    HandleJoin(caller, args);
                    break;
                case "leave":
                    HandleLeave(caller);
                    break;
                case "info":
                    HandleInfo(caller);
                    break;
                case "leaderboard":
                    HandleLeaderboard(caller, args);
                    break;
                case "disband":
                    HandleDisband(caller, args);
                    break;
                case "kick":
                    HandleKick(caller, args);
                    break;
                case "transfer":
                    HandleTransfer(caller, args);
                    break;
                case "stats":
                    HandleStats(caller, args);
                    break;
                default:
                    pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                    break;
            }
        }

        private UnturnedPlayer GetUnturnedPlayer(IRocketPlayer caller)
        {
            return (UnturnedPlayer)caller;
        }

        private void HandleCreate(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                return;
            }

            string groupName = args[0];

            if (groupName.Length < configuration.GroupNameMinLength || groupName.Length > configuration.GroupNameMaxLength)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupCreateFailNameLength",
                    configuration.GroupNameMinLength, configuration.GroupNameMaxLength);
                return;
            }

            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();
            if (component == null || !component.Loaded)
            {
                pluginInstance.SendMessageToPlayer(caller, "PlayerStatsNotLoaded", player.DisplayName);
                return;
            }

            if (!string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupCreateFailAlreadyInGroup");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group existing = pluginInstance.GroupDatabase.GetGroupByName(groupName);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (existing != null)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupCreateFailNameTaken", groupName);
                        return;
                    }

                    string groupId = Guid.NewGuid().ToString("N").Substring(0, 16);
                    Group group = new()
                    {
                        GroupId = groupId,
                        GroupName = groupName,
                        OwnerSteamId = component.SteamId,
                        Members = new List<ulong> { component.SteamId },
                        CreatedAt = DateTime.UtcNow
                    };

                    component.PlayerData.GroupId = groupId;

                    ThreadHelper.RunAsynchronously(() =>
                    {
                        pluginInstance.GroupDatabase.AddOrUpdateGroup(group);
                        pluginInstance.Database.AddOrUpdatePlayer(component.PlayerData);
                    });

                    pluginInstance.SendMessageToPlayer(caller, "GroupCreateSuccess", groupName);
                });
            });
        }

        private void HandleInvite(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                return;
            }

            string targetName = args[0];
            UnturnedPlayer callerPlayer = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = callerPlayer.Player.GetComponent<PlayerStatsComponent>();

            if (component == null || string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupInfoEmpty");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null || group.OwnerSteamId != component.SteamId)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInviteFailNotOwner");
                        return;
                    }

                    UnturnedPlayer targetPlayer = UnturnedPlayer.FromName(targetName);
                    if (targetPlayer == null)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInviteFailPlayerNotFound", targetName);
                        return;
                    }

                    if (targetPlayer.CSteamID.m_SteamID == component.SteamId)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInviteFailNotAllowed");
                        return;
                    }

                    PlayerStatsComponent targetComponent = targetPlayer.Player.GetComponent<PlayerStatsComponent>();
                    if (targetComponent == null || !string.IsNullOrEmpty(targetComponent.PlayerData.GroupId))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInviteFailAlreadyInGroup", targetName);
                        return;
                    }

                    if (group.Members.Count >= configuration.MaxGroupSize)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInviteFailFull", configuration.MaxGroupSize);
                        return;
                    }

                    if (group.InvitedPlayers.ContainsKey(targetComponent.SteamId))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInviteFailAlreadyInvited", targetName);
                        return;
                    }

                    DateTime expiry = DateTime.UtcNow.AddSeconds(configuration.InviteTimeoutSeconds);
                    group.InvitedPlayers[targetComponent.SteamId] = expiry;

                    ThreadHelper.RunAsynchronously(() =>
                    {
                        pluginInstance.GroupDatabase.AddOrUpdateGroup(group);
                    });

                    pluginInstance.SendMessageToPlayer(caller, "GroupInviteSuccess", targetName, configuration.InviteTimeoutSeconds);
                    pluginInstance.SendMessageToPlayer(targetPlayer, "GroupInviteReceived", callerPlayer.DisplayName, group.GroupName);
                });
            });
        }

        private void HandleJoin(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                return;
            }

            string groupName = args[0];
            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();

            if (component == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupJoinFailInGroup");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroupByName(groupName);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupJoinFailNoInvite", groupName);
                        return;
                    }

                    ulong steamId = component.SteamId;
                    if (!group.InvitedPlayers.TryGetValue(steamId, out DateTime expiry))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupJoinFailNoInvite", groupName);
                        return;
                    }

                    if (DateTime.UtcNow > expiry)
                    {
                        group.InvitedPlayers.Remove(steamId);
                        ThreadHelper.RunAsynchronously(() => pluginInstance.GroupDatabase.AddOrUpdateGroup(group));
                        pluginInstance.SendMessageToPlayer(caller, "GroupJoinFailExpired", groupName);
                        return;
                    }

                    group.Members.Add(steamId);
                    group.InvitedPlayers.Remove(steamId);
                    component.PlayerData.GroupId = group.GroupId;

                    ThreadHelper.RunAsynchronously(() =>
                    {
                        pluginInstance.GroupDatabase.AddOrUpdateGroup(group);
                        pluginInstance.Database.AddOrUpdatePlayer(component.PlayerData);
                    });

                    pluginInstance.SendMessageToPlayer(caller, "GroupJoinSuccess", group.GroupName, group.Members.Count);
                });
            });
        }

        private void HandleLeave(IRocketPlayer caller)
        {
            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();

            if (component == null || string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupLeaveFailNotInGroup");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null)
                    {
                        component.PlayerData.GroupId = null;
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaveFailNotInGroup");
                        return;
                    }

                    ulong steamId = component.SteamId;
                    group.Members.Remove(steamId);
                    component.PlayerData.GroupId = null;

                    if (group.Members.Count == 0)
                    {
                        ThreadHelper.RunAsynchronously(() =>
                        {
                            pluginInstance.GroupDatabase.DeleteGroup(group.GroupId);
                            pluginInstance.Database.AddOrUpdatePlayer(component.PlayerData);
                        });
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaveOwnerDestroyed");
                        return;
                    }

                    if (group.OwnerSteamId == steamId)
                    {
                        group.OwnerSteamId = group.Members[0];

                        UnturnedPlayer newOwner = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(group.OwnerSteamId));
                        string newOwnerName = newOwner?.DisplayName ?? group.OwnerSteamId.ToString();

                        ThreadHelper.RunAsynchronously(() =>
                        {
                            pluginInstance.GroupDatabase.AddOrUpdateGroup(group);
                            pluginInstance.Database.AddOrUpdatePlayer(component.PlayerData);
                        });

                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaveOwnershipTransferred", newOwnerName);
                    }
                    else
                    {
                        ThreadHelper.RunAsynchronously(() =>
                        {
                            pluginInstance.GroupDatabase.AddOrUpdateGroup(group);
                            pluginInstance.Database.AddOrUpdatePlayer(component.PlayerData);
                        });
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaveSuccess");
                    }
                });
            });
        }

        private void HandleInfo(IRocketPlayer caller)
        {
            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();

            if (component == null || string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupInfoEmpty");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                GroupRanking ranking = pluginInstance.GroupDatabase.GetGroupRank(group.GroupId, configuration.ActualStatsMode);

                List<(ulong steamId, PlayerStatsData stats)> memberStats = new();
                foreach (ulong memberId in group.Members)
                {
                    PlayerStatsData stats = pluginInstance.Database.GetPlayer(memberId);
                    if (stats != null)
                    {
                        memberStats.Add((memberId, stats));
                    }
                }

                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupInfoEmpty");
                        return;
                    }

                    UnturnedPlayer owner = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(group.OwnerSteamId));
                    string ownerName = owner?.DisplayName ?? group.OwnerSteamId.ToString();
                    string created = group.CreatedAt.ToString("yyyy-MM-dd");
                    string rankStr = ranking != null ? "#" + ranking.Rank.ToString() : "-";

                    pluginInstance.SendMessageToPlayer(caller, "GroupInfoHeader",
                        $"{group.GroupName} [{rankStr}]", ownerName, group.Members.Count, configuration.MaxGroupSize, created);

                    if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                    {
                        List<(ulong steamId, PlayerStatsData stats)> sorted = memberStats
                            .OrderByDescending(m => m.stats.Kills)
                            .ToList();

                        for (int i = 0; i < sorted.Count; i++)
                        {
                            string name = GetPlayerName(sorted[i].steamId, sorted[i].stats.Name);
                            string kills = sorted[i].stats.Kills.ToString("N0");
                            string deaths = sorted[i].stats.Deaths.ToString("N0");
                            string kdr = sorted[i].stats.Deaths == 0
                                ? sorted[i].stats.Kills.ToString("N2")
                                : ((decimal)sorted[i].stats.Kills / sorted[i].stats.Deaths).ToString("N2");

                            pluginInstance.SendMessageToPlayer(caller, "GroupInfoMemberPVP",
                                (i + 1).ToString(), name, kills, deaths, kdr);
                        }
                    }
                    else
                    {
                        List<(ulong steamId, PlayerStatsData stats)> sorted = memberStats
                            .OrderByDescending(m => m.stats.Zombies)
                            .ToList();

                        for (int i = 0; i < sorted.Count; i++)
                        {
                            string name = GetPlayerName(sorted[i].steamId, sorted[i].stats.Name);
                            string zombies = sorted[i].stats.Zombies.ToString("N0");
                            string animals = sorted[i].stats.Animals.ToString("N0");

                            pluginInstance.SendMessageToPlayer(caller, "GroupInfoMemberPVE",
                                (i + 1).ToString(), name, zombies, animals);
                        }
                    }
                });
            });
        }

        private void HandleLeaderboard(IRocketPlayer caller, string[] args)
        {
            int page = 1;
            if (args.Length > 0)
            {
                int.TryParse(args[0], out page);
                if (page < 1) page = 1;
            }

            int pageSize = configuration.GroupLeaderboardPageSize;
            int offset = (page - 1) * pageSize;

            ThreadHelper.RunAsynchronously(() =>
            {
                List<GroupRanking> rankings = pluginInstance.GroupDatabase.GetGroupLeaderboard(
                    configuration.ActualStatsMode, pageSize, offset);

                ThreadHelper.RunSynchronously(() =>
                {
                    if (rankings.Count == 0 && page == 1)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaderboardEmpty");
                        return;
                    }

                    if (rankings.Count == 0)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaderboardNoPage", page, 1);
                        return;
                    }

                    if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaderboardHeaderPVP",
                            rankings.Count, page, page);
                        foreach (GroupRanking r in rankings)
                        {
                            pluginInstance.SendMessageToPlayer(caller, "GroupLeaderboardItem",
                                r.Rank.ToString(), r.GroupName, r.TotalKills.ToString("N0"), r.MemberCount);
                        }
                    }
                    else
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupLeaderboardHeaderPVE",
                            rankings.Count, page, page);
                        foreach (GroupRanking r in rankings)
                        {
                            pluginInstance.SendMessageToPlayer(caller, "GroupLeaderboardItemPVE",
                                r.Rank.ToString(), r.GroupName, r.TotalZombieKills.ToString("N0"), r.MemberCount);
                        }
                    }
                });
            });
        }

        private void HandleDisband(IRocketPlayer caller, string[] args)
        {
            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();

            if (component == null || string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupDisbandFailNotOwner");
                return;
            }

            if (args.Length > 0 && args[0].ToLower() == "confirm")
            {
                ulong steamId = component.SteamId;
                lock (disbandConfirmations)
                {
                    if (!disbandConfirmations.TryGetValue(steamId, out DateTime expiry) || DateTime.UtcNow > expiry)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupDisbandExpired");
                        return;
                    }
                    disbandConfirmations.Remove(steamId);
                }

                ThreadHelper.RunAsynchronously(() =>
                {
                    Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                    if (group == null || group.OwnerSteamId != component.SteamId)
                    {
                        ThreadHelper.RunSynchronously(() => pluginInstance.SendMessageToPlayer(caller, "GroupDisbandFailNotOwner"));
                        return;
                    }

                    string groupName = group.GroupName;
                    List<ulong> members = new(group.Members);

                    pluginInstance.GroupDatabase.DeleteGroup(group.GroupId);

                    foreach (ulong memberId in members)
                    {
                        PlayerStatsData memberData = pluginInstance.Database.GetPlayer(memberId);
                        if (memberData != null)
                        {
                            memberData.GroupId = null;
                            pluginInstance.Database.AddOrUpdatePlayer(memberData);
                        }
                    }

                    ThreadHelper.RunSynchronously(() =>
                    {
                        component.PlayerData.GroupId = null;
                        pluginInstance.SendMessageToPlayer(caller, "GroupDisbandConfirmed", groupName);
                    });
                });
            }
            else
            {
                ThreadHelper.RunAsynchronously(() =>
                {
                    Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                    ThreadHelper.RunSynchronously(() =>
                    {
                        if (group == null || group.OwnerSteamId != component.SteamId)
                        {
                            pluginInstance.SendMessageToPlayer(caller, "GroupDisbandFailNotOwner");
                            return;
                        }

                        lock (disbandConfirmations)
                        {
                            disbandConfirmations[component.SteamId] = DateTime.UtcNow.AddSeconds(5);
                        }
                        pluginInstance.SendMessageToPlayer(caller, "GroupDisbandConfirm");
                    });
                });
            }
        }

        private void HandleKick(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                return;
            }

            string targetName = args[0];
            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();

            if (component == null || string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupKickFailNotOwner");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null || group.OwnerSteamId != component.SteamId)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupKickFailNotOwner");
                        return;
                    }

                    UnturnedPlayer target = UnturnedPlayer.FromName(targetName);
                    ulong targetId;
                    if (target != null)
                    {
                        targetId = target.CSteamID.m_SteamID;
                    }
                    else if (!ulong.TryParse(targetName, out targetId))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "PlayerNotFound", targetName);
                        return;
                    }

                    if (targetId == component.SteamId)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupKickFailSelf");
                        return;
                    }

                    if (!group.Members.Contains(targetId))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupKickFailNotMember", targetName);
                        return;
                    }

                    group.Members.Remove(targetId);

                    ThreadHelper.RunAsynchronously(() =>
                    {
                        pluginInstance.GroupDatabase.AddOrUpdateGroup(group);

                        PlayerStatsData targetData = pluginInstance.Database.GetPlayer(targetId);
                        if (targetData != null)
                        {
                            targetData.GroupId = null;
                            pluginInstance.Database.AddOrUpdatePlayer(targetData);
                        }

                        ThreadHelper.RunSynchronously(() =>
                        {
                            UnturnedPlayer onlineTarget = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(targetId));
                            if (onlineTarget != null)
                            {
                                PlayerStatsComponent targetComponent = onlineTarget.Player.GetComponent<PlayerStatsComponent>();
                                if (targetComponent != null)
                                {
                                    targetComponent.PlayerData.GroupId = null;
                                }
                            }

                            pluginInstance.SendMessageToPlayer(caller, "GroupKickSuccess",
                                target?.DisplayName ?? targetName);
                        });
                    });
                });
            });
        }

        private void HandleTransfer(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                return;
            }

            string targetName = args[0];
            UnturnedPlayer player = GetUnturnedPlayer(caller);
            PlayerStatsComponent component = player.Player.GetComponent<PlayerStatsComponent>();

            if (component == null || string.IsNullOrEmpty(component.PlayerData.GroupId))
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupTransferFailNotOwner");
                return;
            }

            ThreadHelper.RunAsynchronously(() =>
            {
                Group group = pluginInstance.GroupDatabase.GetGroup(component.PlayerData.GroupId);
                ThreadHelper.RunSynchronously(() =>
                {
                    if (group == null || group.OwnerSteamId != component.SteamId)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupTransferFailNotOwner");
                        return;
                    }

                    UnturnedPlayer target = UnturnedPlayer.FromName(targetName);
                    ulong targetId;
                    if (target != null)
                    {
                        targetId = target.CSteamID.m_SteamID;
                    }
                    else if (!ulong.TryParse(targetName, out targetId))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "PlayerNotFound", targetName);
                        return;
                    }

                    if (targetId == component.SteamId)
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupTransferFailSelf");
                        return;
                    }

                    if (!group.Members.Contains(targetId))
                    {
                        pluginInstance.SendMessageToPlayer(caller, "GroupTransferFailNotMember", targetName);
                        return;
                    }

                    group.OwnerSteamId = targetId;

                    ThreadHelper.RunAsynchronously(() =>
                    {
                        pluginInstance.GroupDatabase.AddOrUpdateGroup(group);
                    });

                    pluginInstance.SendMessageToPlayer(caller, "GroupTransferSuccess",
                        target?.DisplayName ?? targetName);
                });
            });
        }

        private void HandleStats(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 0)
            {
                pluginInstance.SendMessageToPlayer(caller, "GroupSyntax");
                return;
            }

            string targetName = args[0];

            CommandHelper.GetPlayerData(caller, new[] { targetName }, (playerData) =>
            {
                if (string.IsNullOrEmpty(playerData.GroupId))
                {
                    pluginInstance.SendMessageToPlayer(caller, "GroupStatsFailNoGroup", playerData.Name);
                    return;
                }

                ThreadHelper.RunAsynchronously(() =>
                {
                    Group group = pluginInstance.GroupDatabase.GetGroup(playerData.GroupId);
                    ThreadHelper.RunSynchronously(() =>
                    {
                        if (group == null)
                        {
                            pluginInstance.SendMessageToPlayer(caller, "GroupStatsFailNoGroup", playerData.Name);
                            return;
                        }

                        pluginInstance.SendMessageToPlayer(caller, "GroupStatsHeader",
                            group.GroupName, group.Members.Count, configuration.MaxGroupSize);

                        ThreadHelper.RunAsynchronously(() =>
                        {
                            List<(ulong steamId, PlayerStatsData stats)> memberStats = new();
                            foreach (ulong memberId in group.Members)
                            {
                                PlayerStatsData memberData = pluginInstance.Database.GetPlayer(memberId);
                                if (memberData != null)
                                {
                                    memberStats.Add((memberId, memberData));
                                }
                            }

                            ThreadHelper.RunSynchronously(() =>
                            {
                                if (configuration.ActualStatsMode == StatsMode.Both || configuration.ActualStatsMode == StatsMode.PVP)
                                {
                                    List<(ulong steamId, PlayerStatsData stats)> sorted = memberStats
                                        .OrderByDescending(m => m.stats.Kills).ToList();
                                    for (int i = 0; i < sorted.Count; i++)
                                    {
                                        string name = GetPlayerName(sorted[i].Item1, sorted[i].stats.Name);
                                        string kills = sorted[i].stats.Kills.ToString("N0");
                                        string deaths = sorted[i].stats.Deaths.ToString("N0");
                                        string kdr = sorted[i].stats.Deaths == 0
                                            ? sorted[i].stats.Kills.ToString("N2")
                                            : ((decimal)sorted[i].stats.Kills / sorted[i].stats.Deaths).ToString("N2");

                                        pluginInstance.SendMessageToPlayer(caller, "GroupInfoMemberPVP",
                                            (i + 1).ToString(), name, kills, deaths, kdr);
                                    }
                                }
                                else
                                {
                                    List<(ulong steamId, PlayerStatsData stats)> sorted = memberStats
                                        .OrderByDescending(m => m.stats.Zombies).ToList();
                                    for (int i = 0; i < sorted.Count; i++)
                                    {
                                        string name = GetPlayerName(sorted[i].Item1, sorted[i].stats.Name);
                                        string zombies = sorted[i].stats.Zombies.ToString("N0");
                                        string animals = sorted[i].stats.Animals.ToString("N0");

                                        pluginInstance.SendMessageToPlayer(caller, "GroupInfoMemberPVE",
                                            (i + 1).ToString(), name, zombies, animals);
                                    }
                                }
                            });
                        });
                    });
                });
            });
        }

        private string GetPlayerName(ulong steamId, string fallback)
        {
            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(steamId));
            return player?.DisplayName ?? fallback ?? steamId.ToString();
        }
    }
}
