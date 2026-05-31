# Player Stats
> Track and display player PVP and PVE statistics with rankings, groups, rewards and UI

---

## Overview
Player Stats is a plugin that tracks various player statistics and provides rankings for both PVP and PVE gameplay. The plugin supports player groups, permission-based rewards and an optional visual UI.

## Features
| Feature | Description |
|---------|-------------|
| 💾 Data Storage | Store player stats in a JSON file or MySQL database |
| 📈 Stats Display | Show player stats, playtime, ranking, and session stats via commands |
| 🏆 Rankings | Support for both PVP (Kills) and PVE (Zombie Kills) leaderboards |
| 👥 Groups | Create player groups with invites, ownership management, group stats and leaderboards |
| 🎁 Rewards | Permission group rewards for reaching specific kill thresholds |
| 🔄 Migration | Automatic migration from Arechi PlayerStats plugin |
| 🖥️ User Interface | Optional UI for viewing PVP/PVE stats and group information |

## Workshop (Optional)
The UI is optional and provides a visual display for player stats and group information.

- **Workshop Item**: [Player Stats UI](https://steamcommunity.com/sharedfiles/filedetails/?id=3352126593)
- **ID**: `3352126593`

> 💡 **PRO TIP**  
> Remember to set `<EnableUIEffect>true</EnableUIEffect>` in the configuration file to activate the UI.

The UI displays the player's current group and group rank. Players can also use `/groupui` to open or close the group menu panel.

## Tracked Statistics

### PVP Stats
- Kills
- Deaths
- KDR (Kill/Death Ratio)
- HS% (Headshot Percentage)

### PVE Stats
- Zombies (zombies killed)
- Mega Zombies (mega zombies killed)
- Animals (animals killed)
- Resources (resources gathered)
- Harvests (plant harvests)
- Fish (fish caught)

---

## Configuration Options

### Stats Mode (`<StatsMode>Both</StatsMode>`)

| StatsMode | Description |
|------|-------------|
| `Both` | The `/stats` command displays both PVP and PVE stats, but ranking, rewards and UI are based on PVP stats |
| `PVP` | The `/stats` command displays only PVP stats, and ranking, rewards and UI are based on PVP stats |
| `PVE` | The `/stats` command displays only PVE stats, and ranking, rewards and UI are based on PVE stats |

### Automatic Bans

The Automatic Bans feature allows server owners to automatically ban players who meet suspicious statistical patterns, helping detect potential cheaters or rule violators. Bans are triggered when players die or eliminate other players, checking their overall statistics against configured thresholds.

**How it works:**
- Conditions are checked whenever a player kills someone or dies
- Multiple conditions can be combined - ALL conditions must be met to trigger a ban
- Supports custom ban reasons that will be displayed to the banned player
- Uses the player's total statistics (not session stats) for evaluation

**Available Statistics:**
- `Kills` - Total player kills
- `Deaths` - Total deaths (PVP and/or PVE based on ShowCombinedDeaths setting)
- `PVPDeaths` - Deaths caused by other players only
- `Headshots` - Total headshot kills
- `Accuracy` - Headshot percentage (0-100)
- `Playtime` - Total time played on the server (in seconds)

**Supported Comparers:**
- `greater` - Greater than the specified value
- `less` - Less than the specified value  
- `equal` - Equal to the specified value

**Example Configuration:**
The default configuration automatically bans players who achieve more than 30 kills with over 80% headshot accuracy within their first hour of playtime - a pattern typically indicating cheating:

```xml
<AutomaticBan Reason="Cheating (AB)">
  <Conditions>
    <Condition Comparer="greater" Stat="Kills" Value="30" />
    <Condition Comparer="greater" Stat="Accuracy" Value="80" />
    <Condition Comparer="less" Stat="Playtime" Value="3600" />
  </Conditions>
</AutomaticBan>
```

> ⚠️ **Warning**  
> Use this feature carefully and test your conditions thoroughly. Consider your server's gameplay style and player skill levels when setting thresholds to avoid false positives.

### Groups

Players can create groups, invite members and compete on a separate group leaderboard. A group's score is the combined score of its members:

| Stats Mode | Group leaderboard score |
|------------|-------------------------|
| `Both` | Total player kills |
| `PVP` | Total player kills |
| `PVE` | Total zombie kills |

Group names are unique and case-insensitive. Invites expire automatically. Only the group owner can invite players, kick members, transfer ownership or disband the group. If an owner leaves a group with other members, ownership is transferred automatically. If the last member leaves, the group is deleted.

Groups use the configured database provider:

- With `json`, groups and pending invites are stored in `Groups.json` next to the configured player stats JSON file.
- With `mysql`, the plugin automatically creates the `<PlayerStatsTableName>_groups` and `<PlayerStatsTableName>_group_invites` tables and adds the `GroupId` column to the player stats table when needed.

### Other Options

The plugin offers several additional options to customize functionality:

| Option | Description |
|--------|-------------|
| `DatabaseProvider` | Choose between `json` or `mysql` for data storage |
| `SaveIntervalSeconds` | Interval in seconds for saving player stats from memory to the database |
| `ShowCombinedDeaths` | When enabled, deaths in statistics count both PVE (zombies, animals, suicides) and PVP deaths. When disabled, only deaths matching the current `StatsMode` are counted |
| `EnableUIEffect` | Activates the optional UI for viewing statistics. Make sure to install the workshop mod to use it |
| `ShowUIEffectByDefault` | Whether the UI should be shown by default when players join. Players can toggle it using `/statsui` command |
| `EnableJoinLeaveMessages` | Displays messages when players join or leave the server (with their rank if they reached a minimum ranking treshold) |
| `MinimumRankingTreshold` | Minimum number of kills required to appear on the ranking |
| `EnableRewards` | Activates the permission-based rewards system |
| `MaxGroupSize` | Maximum number of members allowed in a group |
| `InviteTimeoutSeconds` | Number of seconds before a group invite expires |
| `GroupNameMinLength` | Minimum allowed group name length |
| `GroupNameMaxLength` | Maximum allowed group name length |
| `GroupLeaderboardPageSize` | Number of groups displayed per leaderboard page |

---

---

## Commands

| Command | Description |
|---------|-------------|
| `/playtime [player]` | Shows your or another player's total playtime |
| `/stats [player]` | Displays your or another player's stats |
| `/rank [player]` | Shows your or another player's ranking |
| `/sstats [player]` | Displays your or another player's session stats (since they joined) |
| `/splaytime [player]` | Shows your or another player's session playtime (since they joined) |
| `/ranking` | Displays the top players ranking |
| `/statsui` | Toggles the stats UI on/off |
| `/group` | Shows the group command menu |
| `/groupcommands` or `/gcmds` | Shows the group command menu |
| `/groupui` | Toggles the optional group UI panel |

### Group Commands

| Command | Description |
|---------|-------------|
| `/group create <name>` | Creates a new group |
| `/group invite <player>` | Invites an online player. Group owner only |
| `/group join <name>` | Accepts a pending invite to a group |
| `/group leave` | Leaves the current group |
| `/group info` | Displays the current group's rank, owner, creation date and member stats |
| `/group leaderboard [page]` | Displays the group leaderboard |
| `/group stats <player>` | Displays stats for the specified player's group |
| `/group kick <player-or-steamid>` | Removes a member from the group. Group owner only |
| `/group transfer <player-or-steamid>` | Transfers ownership to another member. Group owner only |
| `/group disband` | Starts group disband confirmation. Group owner only |
| `/group disband confirm` | Confirms disbanding within 5 seconds |

---

## Permissions

```xml
<Permission Cooldown="0">playtime</Permission>
<Permission Cooldown="0">stats</Permission>
<Permission Cooldown="0">rank</Permission>
<Permission Cooldown="0">sstats</Permission>
<Permission Cooldown="0">splaytime</Permission>
<Permission Cooldown="0">ranking</Permission>
<Permission Cooldown="0">statsui</Permission>
<Permission Cooldown="0">group</Permission>
<Permission Cooldown="0">groupcommands</Permission>
<Permission Cooldown="0">groupui</Permission>
```

---

## Configuration
```xml
<?xml version="1.0" encoding="utf-8"?>
<PlayerStatsConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <MessageColor>yellow</MessageColor>
  <MessageIconUrl>https://i.imgur.com/TWjBtCA.png</MessageIconUrl>
  <DatabaseProvider>json</DatabaseProvider>
  <JsonFilePath>{rocket_directory}/Plugins/PlayerStats/PlayerStats.json</JsonFilePath>
  <MySQLConnectionString>Server=127.0.0.1;Port=3306;Database=unturned;Uid=root;Pwd=passw;</MySQLConnectionString>
  <PlayerStatsTableName>PlayerStats</PlayerStatsTableName>
  <SaveIntervalSeconds>300</SaveIntervalSeconds>
  <EnableUIEffect>false</EnableUIEffect>
  <UIEffectId>22512</UIEffectId>
  <ShowUIEffectByDefault>true</ShowUIEffectByDefault>
  <EnableJoinLeaveMessages>true</EnableJoinLeaveMessages>
  <StatsMode>Both</StatsMode>
  <ShowCombinedDeaths>true</ShowCombinedDeaths>
  <MinimumRankingTreshold>25</MinimumRankingTreshold>
  <MaxGroupSize>10</MaxGroupSize>
  <InviteTimeoutSeconds>120</InviteTimeoutSeconds>
  <GroupNameMinLength>3</GroupNameMinLength>
  <GroupNameMaxLength>16</GroupNameMaxLength>
  <GroupLeaderboardPageSize>10</GroupLeaderboardPageSize>
  <EnableRewards>true</EnableRewards>
  <Rewards>
    <Reward Name="VIP Rank" Treshold="50" PermissionGroup="vip" />
    <Reward Name="MVP Rank" Treshold="125" PermissionGroup="mvp" />
  </Rewards>
  <EnableAutomaticBans>false</EnableAutomaticBans>
  <AutomaticBans>
    <AutomaticBan Reason="Cheating (AB)">
      <Conditions>
        <Condition Comparer="greater" Stat="Kills" Value="30" />
        <Condition Comparer="greater" Stat="Accuracy" Value="80" />
        <Condition Comparer="less" Stat="Playtime" Value="3600" />
      </Conditions>
    </AutomaticBan>
  </AutomaticBans>
</PlayerStatsConfiguration>
```

---

## Public API

Other plugins can use `RestoreMonarchy.PlayerStats.APIs.PlayerStatsAPI` to read player and group data.

| Method | Description |
|--------|-------------|
| `IsPlayerInGroup(ulong steamId)` | Returns whether a player belongs to a group |
| `GetPlayerGroupId(ulong steamId)` | Returns a player's group ID |
| `GetPlayerGroup(ulong steamId, Action<Group> callback)` | Gets a player's group |
| `GetGroupById(string groupId, Action<Group> callback)` | Gets a group by ID |
| `GetGroupByName(string groupName, Action<Group> callback)` | Gets a group by name |
| `GetGroupLeaderboard(bool pvp, int limit, int offset, Action<List<GroupRanking>> callback)` | Gets a paginated PVP or PVE group leaderboard |
| `GetGroupRank(string groupId, bool pvp, Action<GroupRanking> callback)` | Gets a group's PVP or PVE rank |
| `GetGroupMembers(string groupId, Action<List<PlayerStatsData>> callback)` | Gets stats for all members of a group |
| `GetGroupMemberCount(string groupId)` | Returns the number of members in a group |

The callback-based methods perform database work asynchronously and invoke their callbacks on the main thread.

---

## Credits
UI made by **💪 Soer (Unbeaten)**. He also sponsored the creation of this plugin 💸.

---

## Frequently Asked Questions

1. **How do I enable the UI?**  
   Set `<EnableUIEffect>true</EnableUIEffect>` in the configuration and subscribe to the workshop item.

2. **Can I use MySQL instead of JSON?**  
   Yes, change `<DatabaseProvider>json</DatabaseProvider>` to `<DatabaseProvider>mysql</DatabaseProvider>` and configure your connection string.

3. **How do I customize rewards?**  
   Edit the `<Rewards>` section in the configuration file with your desired thresholds and permission groups.

4. **How can I create a custom UI?**  
   You can download the `statsui.unitypackage` from the **All versions** page and import it into your Unity project. Then, customize the UI as needed and reupload it to the workshop. Remember in Unity do not rename any of the objects in the hierarchy, as the plugin uses the object names to find them. When reuploading, make sure to use a unique GUID and ID.

5. **Do I need to create database tables for groups manually?**
   No. The plugin creates the required MySQL tables and adds the player `GroupId` column automatically. JSON installations automatically create `Groups.json`.

6. **How are groups ranked?**
   In `Both` and `PVP` modes, groups are ranked by their members' combined player kills. In `PVE` mode, groups are ranked by combined zombie kills.

---

*For support, bug reports, or feature requests, please write on our forum or join our Discord.*
