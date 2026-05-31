using RestoreMonarchy.PlayerStats.Components;
using RestoreMonarchy.PlayerStats.Databases;
using RestoreMonarchy.PlayerStats.Helpers;
using RestoreMonarchy.PlayerStats.Models;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RestoreMonarchy.PlayerStats
{
    public class PlayerStatsPlugin : RocketPlugin<PlayerStatsConfiguration>
    {
        public static PlayerStatsPlugin Instance { get; private set; }
        public UnityEngine.Color MessageColor { get; set; }
        public IDatabase Database { get; private set; }
        public IGroupDatabase GroupDatabase { get; private set; }

        protected override void Load()
        {
            Instance = this;
            MessageColor = UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor, UnityEngine.Color.green);

            if (Configuration.Instance.StatsMode == null)
            {
                MigrateToNewFormat();
            }

            if (Configuration.Instance.DatabaseProvider.Equals("mysql", StringComparison.OrdinalIgnoreCase))
            {
                Database = new MySQLDatabase();
                GroupDatabase = new MySQLGroupDatabase();
                Logger.Log("Database provider is set to MySQL");
            } else
            {
                Database = new JsonDatabase();
                GroupDatabase = new JsonGroupDatabase();
                Logger.Log("Database provider is set to JSON");
            }            
            Database.Initialize();
            GroupDatabase.Initialize();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            PlayerLife.onPlayerDied += OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerUpdateStat += OnPlayerUpdatedStat;
            StructureManager.onStructureSpawned += OnStructureSpawned;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Provider.onCommenceShutdown += onCommenceShutdown;
            Provider.onServerShutdown += OnServerShutdown;

            foreach (Player player in PlayerTool.EnumeratePlayers())
            {
                player.gameObject.AddComponent<PlayerStatsComponent>(); 
            }

            InvokeRepeating(nameof(Save), Configuration.Instance.SaveIntervalSeconds, Configuration.Instance.SaveIntervalSeconds);
            InvokeRepeating(nameof(CleanupExpiredInvites), 60, 60);

            Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} has been loaded!", ConsoleColor.Yellow);
            Logger.Log($"Check out more Unturned plugins at restoremonarchy.com");
        }

        public void MigrateToNewFormat()
        {
            // Convert legacy settings to new StatsMode
            Configuration.Instance.StatsMode = Configuration.Instance.ActualStatsMode.ToString();
            Configuration.Instance.actualStatsMode = null;

            // Clean up legacy settings
            Configuration.Instance.EnablePVPStats = true;
            Configuration.Instance.EnablePVEStats = true;
            Configuration.Instance.PVPRanking = true;
            Configuration.Instance.PVPRewards = Configuration.Instance.EnableRewards;
            Configuration.Instance.PVPUI = true;

            Configuration.Save();
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            PlayerLife.onPlayerDied -= OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerUpdateStat -= OnPlayerUpdatedStat;
            StructureManager.onStructureSpawned -= OnStructureSpawned;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            Provider.onCommenceShutdown -= onCommenceShutdown;
            Provider.onServerShutdown -= OnServerShutdown;

            CancelInvoke(nameof(Save));
            CancelInvoke(nameof(CleanupExpiredInvites));
            Save(false);

            foreach (Player player in PlayerTool.EnumeratePlayers())
            {
                PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
                Destroy(component);
            }

            Logger.Log($"{Name} has been unloaded!", ConsoleColor.Yellow);
        }

        public override TranslationList DefaultTranslations => new()
        {
            { "StatsCommandSyntax", "You must specify player name or steamID." },
            { "PlayerStatsNotLoaded", "Player stats are not loaded for [[b]]{0}.[[/b]] Please try again later." },
            { "PlayerNotFound", "Player [[b]]{0}[[/b]] not found." },
            { "YourPVPStats", "[[b]]Your[[/b]] PVP stats | Kills: [[b]]{0}[[/b]], Deaths: [[b]]{1}[[/b]], KDR: [[b]]{2}[[/b]], HS%: [[b]]{3}[[/b]]" },
            { "YourPVEStats", "[[b]]Your[[/b]] PVE stats | Zombies: [[b]]{0}[[/b]], Mega Zombies: [[b]]{1}[[/b]], Animals: [[b]]{2}[[/b]], Resources: [[b]]{3}[[/b]], Harvests: [[b]]{4}[[/b]], Fish: [[b]]{5}[[/b]]" },
            { "OtherPVPStats", "[[b]]{0}[[/b]] PVP stats | Kills: [[b]]{1}[[/b]], Deaths: [[b]]{2}[[/b]], KDR: [[b]]{3}[[/b]], HS%: [[b]]{4}[[/b]]" },
            { "OtherPVEStats", "[[b]]{0}[[/b]] PVE stats | Zombies: [[b]]{1}[[/b]], Mega Zombies: [[b]]{2}[[/b]], Animals: [[b]]{3}[[/b]], Resources: [[b]]{4}[[/b]], Harvests: [[b]]{5}[[/b]], Fish: [[b]]{6}[[/b]]" },
            { "PlaytimeCommandSyntax", "You must specify player name or steamID." },
            { "YourPlaytime", "You have played for [[b]]{0}[[/b]]" },
            { "OtherPlaytime", "[[b]]{0}[[/b]] has played for [[b]]{1}[[/b]]" },
            { "RankCommandSyntax", "You must specify player name or steamID." },
            { "YourPlayerPVPRanking", "Your rank is [[b]]#{0}[[/b]] with {1} kills" },
            { "OtherPlayerPVPRanking", "[[b]]{0}[[/b]] rank is [[b]]#{1}[[/b]] with {2} kills." },
            { "YourPlayerPVERanking", "Your rank is [[b]]#{0}[[/b]] with {1} zombie kills." },
            { "OtherPlayerPVERanking", "[[b]]{0}[[/b]] rank is [[b]]#{1}[[/b]] with {2} zombie kills." },
            { "RankingListHeaderPVP", "[[b]]Top {0} Players by Kills[[/b]]" },
            { "RankingListItemPVP", "[[b]]#{0}[[/b]] [[b]]{1}[[/b]] - {2} kills" },
            { "RankingListHeaderPVE", "[[b]]Top {0} Players by Zombie Kills[[/b]]" },
            { "RankingListItemPVE", "[[b]]#{0}[[/b]] [[b]]{1}[[/b]] - {2} zombie kills" },
            { "YouAreUnrankedPVP", "You are unranked because you have [[b]]{0}/{1}[[/b]] kills. " },
            { "OtherPlayerIsUnrankedPVP", "[[b]]{0}[[/b]] is unranked because they have [[b]]{1}/{2}[[/b]] kills." },
            { "YouAreUnrankedPVE", "You are unranked because you have [[b]]{0}/{1}[[/b]] zombie kills. " },
            { "OtherPlayerIsUnrankedPVE", "[[b]]{0}[[/b]] is unranked because they have [[b]]{1}/{2}[[/b]] zombie kills." },
            { "NoRankingPlayersFound", "There isn't any players qualified for ranking yet." },
            { "StatsUIEffectDisabled", "Stats UI is not enabled on this server." },
            { "StatsUIDisabled", "Stats UI has been disabled" },
            { "StatsUIEnabled", "Stats UI has been enabled" },
            { "RewardReceivedPVP", "You received [[b]]{0}[[/b]] reward for {1} kills." },
            { "RewardReceivedPVE", "You received [[b]]{0}[[/b]] reward for {1} zombie kills." },
            { "YourPVPSessionStats", "[[b]]Your[[/b]] PVP session stats | Kills: [[b]]{0}[[/b]], Deaths: [[b]]{1}[[/b]], KDR: [[b]]{2}[[/b]], HS%: [[b]]{3}[[/b]]" },
            { "OtherPVPSessionStats", "[[b]]{0}[[/b]] PVP session stats | Kills: [[b]]{1}[[/b]], Deaths: [[b]]{2}[[/b]], KDR: [[b]]{3}[[/b]], HS%: [[b]]{4}[[/b]]" },
            { "YourPVESessionStats", "[[b]]Your[[/b]] PVE session stats | Zombies: [[b]]{0}[[/b]], Mega Zombies: [[b]]{1}[[/b]], Animals: [[b]]{2}[[/b]], Resources: [[b]]{3}[[/b]], Harvests: [[b]]{4}[[/b]], Fish: [[b]]{5}[[/b]]" },
            { "OtherPVESessionStats", "[[b]]{0}[[/b]] PVE session stats | Zombies: [[b]]{1}[[/b]], Mega Zombies: [[b]]{2}[[/b]], Animals: [[b]]{3}[[/b]], Resources: [[b]]{4}[[/b]], Harvests: [[b]]{5}[[/b]], Fish: [[b]]{6}[[/b]]" },
            { "SessionStatsCommandSyntax", "You must specify player name or steamID." },
            { "SessionPlaytimeCommandSyntax", "You must specify player name or steamID." },
            { "YourSessionPlaytime", "You have played for [[b]]{0}[[/b]] since you joined." },
            { "OtherSessionPlaytime", "[[b]]{0}[[/b]] has played for [[b]]{1}[[/b]] since they joined." },

            { "JoinMessage", "[[b]][#{0}] {1}[[/b]] joined the server." },
            { "LeaveMessage", "[[b]][#{0}] {1}[[/b]] left the server." },
            { "JoinMessageNoRank", "[[b]]{0}[[/b]] joined the server." },
            { "LeaveMessageNoRank", "[[b]]{0}[[/b]] left the server." },

            { "Day", "1 day" },
            { "Days", "{0} days" },
            { "Hour", "1 hour" },
            { "Hours", "{0} hours" },
            { "Minute", "1 minute" },
            { "Minutes", "{0} minutes" },
            { "Second", "1 second" },
            { "Seconds", "{0} seconds" },
            { "Zero", "a moment" },

            { "UI_NextReward", "Next Reward: {0}" },
            { "UI_RewardProgress", "{0}/{1} Kills" },
            { "UI_Kills", "KILLS" },
            { "UI_Deaths", "DEATHS" },
            { "UI_Headshots", "HS" },
            { "UI_Accuracy", "HS%" },
            { "UI_Rank", "RANK" },
            { "UI_KDR", "K/D" },
            { "UI_Footer", "Use /statsui to hide" },

            { "UI_RewardProgressPVE", "{0}/{1} Zombies" },
            { "UI_ZombieKills", "ZOMBIES" },
            { "UI_MegaZombieKills", "MEGAS" },
            { "UI_AnimalKills", "ANIMALS" },
            { "UI_ResourcesGathered", "GATHERS" },
            { "UI_PVEDeaths", "DEATHS" },

            { "GroupCreateSuccess", "Group [[b]]{0}[[/b]] created! Use /group invite <player> to invite members." },
            { "GroupCreateFailNameLength", "Group name must be between {0} and {1} characters." },
            { "GroupCreateFailAlreadyInGroup", "You are already in a group. Use /group leave first." },
            { "GroupCreateFailNameTaken", "A group with the name [[b]]{0}[[/b]] already exists." },
            { "GroupInviteSuccess", "You invited [[b]]{0}[[/b]] to your group. Invite expires in {1} seconds." },
            { "GroupInviteReceived", "[[b]]{0}[[/b]] invited you to join [[b]]{1}[[/b]]. Use /group join {1} to accept." },
            { "GroupInviteFailNotOwner", "You must be the group owner to invite players." },
            { "GroupInviteFailPlayerNotFound", "Player [[b]]{0}[[/b]] not found." },
            { "GroupInviteFailAlreadyInGroup", "[[b]]{0}[[/b]] is already in a group." },
            { "GroupInviteFailAlreadyInvited", "[[b]]{0}[[/b]] already has a pending invite to your group." },
            { "GroupInviteFailFull", "Your group is full ({0}/{0} members)." },
            { "GroupInviteFailNotAllowed", "You cannot invite yourself." },
            { "GroupJoinSuccess", "You joined [[b]]{0}[[/b]]! Group has {1} members." },
            { "GroupJoinFailNoInvite", "You don't have a pending invite to [[b]]{0}[[/b]]." },
            { "GroupJoinFailExpired", "Your invite to [[b]]{0}[[/b]] has expired." },
            { "GroupJoinFailInGroup", "You are already in a group. Use /group leave first." },
            { "GroupLeaveSuccess", "You left the group." },
            { "GroupLeaveConfirm", "Are you sure? Type [[b]]/group leave confirm[[/b]] within 30 seconds to leave your group." },
            { "GroupLeaveExpired", "Leave confirmation expired." },
            { "GroupLeaveFailNotInGroup", "You are not in a group." },
            { "GroupLeaveOwnershipTransferred", "You left the group. Ownership transferred to [[b]]{0}[[/b]]." },
            { "GroupLeaveOwnerDestroyed", "You were the only member. The group has been disbanded." },
            { "GroupInfoHeader", "[[b]]{0}[[/b]] | Owner: [[b]]{1}[[/b]] | Members: {2}" },
            { "GroupInfoTotalPVP", "[[b]]Total:[[/b]] {0} kills, {1} deaths, KDR: {2}" },
            { "GroupInfoTotalPVE", "[[b]]Total:[[/b]] {0} zombie kills, {1} mega kills, {2} deaths" },
            { "GroupInfoMemberPVP", "  #{0} [[b]]{1}[[/b]] - {2} kills, {3} deaths, KDR: {4}" },
            { "GroupInfoMemberPVE", "  #{0} [[b]]{1}[[/b]] - {2} zombie kills, {3} animals" },
            { "GroupInfoEmpty", "You are not in a group." },
            { "GroupLeaderboardHeaderPVP", "[[b]]Group Leaderboard — Top {0} by Kills (Page {1}/{2})[[/b]]" },
            { "GroupLeaderboardHeaderPVE", "[[b]]Group Leaderboard — Top {0} by Zombie Kills (Page {1}/{2})[[/b]]" },
            { "GroupLeaderboardItem", "  #{0} [[b]]{1}[[/b]] - {2} kills | {3} members" },
            { "GroupLeaderboardItemPVE", "  #{0} [[b]]{1}[[/b]] - {2} zombie kills | {3} members" },
            { "GroupLeaderboardEmpty", "No groups have been created yet." },
            { "GroupLeaderboardNoPage", "Page {0} is out of range. Max page: {1}." },
            { "GroupDisbandConfirm", "Are you sure? Type [[b]]/group disband confirm[[/b]] within 30 seconds to disband your group." },
            { "GroupDisbandConfirmed", "Group [[b]]{0}[[/b]] has been disbanded." },
            { "GroupDisbandExpired", "Disband confirmation expired." },
            { "GroupDisbandFailNotOwner", "You must be the group owner to disband." },
            { "GroupKickSuccess", "[[b]]{0}[[/b]] has been kicked from the group." },
            { "GroupKickFailNotOwner", "You must be the group owner to kick members." },
            { "GroupKickFailNotMember", "[[b]]{0}[[/b]] is not a member of your group." },
            { "GroupKickFailSelf", "You cannot kick yourself. Use /group leave instead." },
            { "GroupTransferSuccess", "Ownership transferred to [[b]]{0}[[/b]]." },
            { "GroupTransferFailNotOwner", "You must be the group owner to transfer ownership." },
            { "GroupTransferFailNotMember", "[[b]]{0}[[/b]] is not a member of your group." },
            { "GroupTransferFailSelf", "You are already the owner." },
            { "GroupStatsHeader", "[[b]]{0}[[/b]] group stats | Members: {1}" },
            { "GroupStatsFailNoGroup", "[[b]]{0}[[/b]] is not in a group." },
            { "GroupSyntax", "Usage: /group <create|invite|join|leave|info|leaderboard|disband|kick|transfer|stats>" },
            { "UI_GroupText", "Group: {0} | Rank: #{1}" },
            { "UI_NoGroupText", "No Group" },
            { "UI_GroupPanelTitle", "GROUP MENU" },
            { "UI_GroupPanelFooter", "/groupui to close" },
            { "UI_GroupPanel_Tag", "[{0}]" },
            { "UI_GroupPanel_Members", "{0} members" },
            { "UI_GroupPanel_NoGroup", "NO GROUP" },
            { "GroupCommandsHeader", "[[b]]=== Group Commands ===[[/b]]" },
            { "GroupCommandsNoGroup", "[[b]]No Group:[[/b]] /group create <name>, /group join <name>, /group leaderboard, /group stats <player>" },
            { "GroupCommandsJoined", "[[b]]Member:[[/b]] /group info, /group invite <player>, /group leave, /group leaderboard" },
            { "GroupCommandsOwner", "[[b]]Owner:[[/b]] /group kick <player>, /group transfer <player>, /group disband" },
            { "GroupCommandsFooter", "[[b]]Other:[[/b]] /group (this menu), /groupcommands (same), /groupui (toggle UI)" }

        };

        internal string FormatTimespan(TimeSpan span)
        {
            if (span <= TimeSpan.Zero) return Translate("Zero");

            List<string> items = new();
            if (span.Days > 0)
                items.Add(span.Days == 1 ? Translate("Day") : Translate("Days", span.Days));
            if (span.Hours > 0)
                items.Add(span.Hours == 1 ? Translate("Hour") : Translate("Hours", span.Hours));
            if (items.Count < 2 && span.Minutes > 0)
                items.Add(span.Minutes == 1 ? Translate("Minute") : Translate("Minutes", span.Minutes));
            if (items.Count < 2 && span.Seconds > 0)
                items.Add(span.Seconds == 1 ? Translate("Second") : Translate("Seconds", span.Seconds));

            return string.Join(" ", items);
        }

        private void Save()
        {
            Save(true);
        }

        private void Save(bool async)
        {
            List<PlayerStatsData> playersData = new();

            if (shutdownPlayerStats != null)
            {
                playersData = shutdownPlayerStats;
            } else
            {
                foreach (Player player in PlayerTool.EnumeratePlayers())
                {
                    PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
                    if (component != null && component.Loaded)
                    {
                        playersData.Add(component.PlayerData);
                    }
                }
            }

            if (async)
            {
                ThreadHelper.RunAsynchronously(() => 
                {
                    Database.Save(playersData);
                    GroupDatabase.Save();
                });
            } else
            {
                Database.Save(playersData);
                GroupDatabase.Save();
            }            
        }

        private List<PlayerStatsData> shutdownPlayerStats = null;

        // get players data before shutdown
        private void onCommenceShutdown()
        {
            List<PlayerStatsData> playersData = new();
            foreach (Player player in PlayerTool.EnumeratePlayers())
            {
                PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
                if (component != null && component.Loaded)
                {
                    playersData.Add(component.PlayerData);
                }
            }

            shutdownPlayerStats = playersData;
        }

        private void OnServerShutdown()
        {
            // save after players are already disconnected from the server
            if (shutdownPlayerStats != null)
            {
                Logger.Log("Server is shutting down, saving player stats...", ConsoleColor.Yellow);
                Save(false);
                shutdownPlayerStats = new();
                Logger.Log("Player stats have been saved!", ConsoleColor.Yellow);
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component == null)
            {
                component = player.Player.gameObject.AddComponent<PlayerStatsComponent>();

                if (Configuration.Instance.EnableJoinLeaveMessages)
                {
                    ThreadHelper.RunAsynchronously(() =>
                    {
                        PlayerRanking playerRanking = Database.GetPlayerRanking(component.SteamId);
                        ThreadHelper.RunSynchronously(() =>
                        {
                            if (playerRanking == null || playerRanking.IsUnranked())
                            {
                                SendMessageToPlayer(null, "JoinMessageNoRank", player.CharacterName);
                            }
                            else
                            {
                                SendMessageToPlayer(null, "JoinMessage", playerRanking.Rank.ToString("N0"), player.CharacterName);
                            }
                        });
                    });
                }
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (shutdownPlayerStats != null && shutdownPlayerStats.Exists(x => x.SteamId == player.CSteamID.m_SteamID))
            {
                return;
            }

            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component != null)
            {
                if (component.Loaded)
                {
                    ThreadHelper.RunAsynchronously(() =>
                    {
                        Database.AddOrUpdatePlayer(component.PlayerData);
                    });
                }

                if (Configuration.Instance.EnableJoinLeaveMessages)
                {
                    ThreadHelper.RunAsynchronously(() =>
                    {
                        PlayerRanking playerRanking = Database.GetPlayerRanking(component.SteamId);
                        ThreadHelper.RunSynchronously(() =>
                        {
                            if (playerRanking == null || playerRanking.IsUnranked())
                            {
                                SendMessageToPlayer(null, "LeaveMessageNoRank", player.CharacterName);
                            }
                            else
                            {
                                SendMessageToPlayer(null, "LeaveMessage", playerRanking.Rank, player.CharacterName);
                            }
                        });
                    });
                }
            }
        }

        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            PlayerStatsComponent component = sender.GetComponent<PlayerStatsComponent>();
            if (component != null)
            {
                Player killer = PlayerTool.getPlayer(instigator);
                component.OnPlayerDeath(killer, limb, cause);
            }
        }

        private readonly EPlayerStat[] stats = new EPlayerStat[]
        {
            EPlayerStat.FOUND_FISHES,
            EPlayerStat.KILLS_ANIMALS,
            EPlayerStat.KILLS_ZOMBIES_NORMAL,
            EPlayerStat.KILLS_ZOMBIES_MEGA,
            EPlayerStat.FOUND_RESOURCES,
            EPlayerStat.FOUND_PLANTS
        };

        private void OnPlayerUpdatedStat(UnturnedPlayer player, EPlayerStat stat)
        {
            if (stats.Contains(stat))
            {
                PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
                if (component != null)
                {
                    component.OnPlayerUpdatedStat(stat);
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            BarricadeData barricadeData = drop.GetServersideData();
            if (barricadeData.owner == 0)
            {
                return;
            }

            Player player = PlayerTool.getPlayer(new CSteamID(barricadeData.owner));
            if (player == null)
            {
                return;
            }

            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component != null)
            {
                component.OnBarricadeSpawned();
            }
        }

        private void OnStructureSpawned(StructureRegion region, StructureDrop drop)
        {
            StructureData structureData = drop.GetServersideData();
            if (structureData.owner == 0)
            {
                return;
            }

            Player player = PlayerTool.getPlayer(new CSteamID(structureData.owner));
            if (player == null)
            {
                return;
            }

            PlayerStatsComponent component = player.GetComponent<PlayerStatsComponent>();
            if (component != null)
            {
                component.OnStructureSpawned();
            }
        }

        private void CleanupExpiredInvites()
        {
            List<Group> allGroups = GroupDatabase.GetAllGroups();
            DateTime now = DateTime.UtcNow;

            foreach (Group group in allGroups)
            {
                List<ulong> expired = group.InvitedPlayers
                    .Where(kvp => kvp.Value <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (expired.Count > 0)
                {
                    foreach (ulong steamId in expired)
                    {
                        group.InvitedPlayers.Remove(steamId);
                    }
                    GroupDatabase.AddOrUpdateGroup(group);
                }
            }
        }

        internal void SendMessageToPlayer(IRocketPlayer player, string translationKey, params object[] placeholder)
        {
            string msg = Translate(translationKey, placeholder);
            msg = msg.Replace("[[", "<").Replace("]]", ">");
            if (player is ConsolePlayer)
            {
                msg = msg.Replace("<b>", "").Replace("</b>", "");
                Logger.Log(msg);
                return;
            }

            SteamPlayer steamPlayer = null;
            if (player != null)
            {
                UnturnedPlayer unturnedPlayer = (UnturnedPlayer)player;
                steamPlayer = unturnedPlayer.SteamPlayer();
            }
            
            ChatManager.serverSendMessage(msg, MessageColor, null, steamPlayer, EChatMode.SAY, Configuration.Instance.MessageIconUrl, true);
        }
    }
}