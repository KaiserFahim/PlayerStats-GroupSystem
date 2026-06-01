# Player Stats & Group System
> Track player PVP/PVE statistics with ranking, rewards, UI, and a full group system with leaderboards

---

## Overview
Player Stats & Group System extends PlayerStats to include group management — players can create groups, invite others, compete on group leaderboards, and view group stats through the in-game UI. All with the same JSON/MySQL database support.

## Group System by Kaiser Fahim

The group system extension was designed and integrated by **Kaiser Fahim** into the existing PlayerStats plugin. It adds full group management without disrupting the core stats tracking — players can create groups, compete on leaderboards, and see live group stats through the UI. All group data integrates with the same JSON/MySQL database provider.

Key integrations:
- **Join Snapshot System** — Tracks stats from the moment a player joins, not lifetime
- **Live UI Auto-Refresh** — Group panel updates instantly when any member kills or dies
- **Shared Effect ID** — Group UI uses the same workshop effect (22512) as the stats UI, toggling between them seamlessly
- **Confirmation Flow** — Leave and disband require 30-second confirmations to prevent accidents
- **Auto Transfer** — Ownership transfers to oldest member when the owner leaves
- **Cross-Group Info** — Any player can view any group's stats with `/group info <name>`

---

## Features
| Feature | Description |
|---------|-------------|
| 💾 Data Storage | Store player stats in a JSON file or MySQL database |
| 📈 Stats Display | Show player stats, playtime, ranking, and session stats via commands |
| 🏆 Rankings | Support for both PVP (Kills) and PVE (Zombie Kills) leaderboards |
| 🎁 Rewards | Permission group rewards for reaching specific kill thresholds |
| 🔄 Migration | Automatic migration from Arechi PlayerStats plugin |
| 🖥️ User Interface | Optional UI for viewing stats and group info |
| 👥 Group System | Create groups, invite members, group leaderboard, group UI panel |
| 📊 Group Stats | Post-join aggregated group kills/deaths/KDR with live updates |

## Workshop (Optional)
The UI is optional and provides a visual display for both individual stats and group info.

- **Workshop Item**: [Player Stats UI](https://steamcommunity.com/sharedfiles/filedetails/?id=3352126593)
- **ID**: `3352126593`

> 💡 **PRO TIP**  
> Set `<EnableUIEffect>true</EnableUIEffect>` in the configuration file to activate the UI.

---

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

# Group System

## Overview
The group system allows players to form groups, invite members, and compete on group leaderboards. Group stats are calculated from the moment a player joins — only post-join kills and deaths count toward the group total. The group UI panel shows live aggregated stats for all group members.

## Group Commands

| Command | Description |
|---------|-------------|
| `/group` | Shows the group command menu in chat |
| `/groupui` | Toggles the group UI panel on/off |
| `/group create <name>` | Creates a new group (max 6 characters, unique name) |
| `/group invite <player>` | Invites a player to your group (owner only) |
| `/group join <groupName>` | Accepts a pending group invite |
| `/group leave` | Prompts confirmation to leave your group (`/group leave confirm`) |
| `/group info` | Shows your group's info with member stats and totals |
| `/group info <groupName>` | Shows any group's info |
| `/group leaderboard [page]` | Displays the group leaderboard |
| `/group disband` | Prompts confirmation to disband (`/group disband confirm`, owner only) |
| `/group kick <player>` | Kicks a member from your group (owner only) |
| `/group transfer <player>` | Transfers group ownership to a member |
| `/group stats <player>` | Shows the group stats of another player |
| `/groupcommands` or `/gcmds` | Shows all group commands in chat |

## Group Permissions

Add these to your default group permissions:

```xml
<Permission Cooldown="0">group</Permission>
<Permission Cooldown="0">groupui</Permission>
<Permission Cooldown="0">groupcommands</Permission>
<Permission Cooldown="0">gcmds</Permission>
```

## Group UI Panel

The group UI panel (`/groupui`) shows:
- **Group Name** — as a tag `[NAME]`
- **Member Count** — displayed below the tag
- **Total Kills** — combined PVP kills of all members (post-join only)
- **Total Deaths** — combined PVP + PVE deaths of all members (post-join only)
- **K/D** — group-wide kill/death ratio

The panel auto-refreshes when any group member kills or dies. No need to re-toggle.

## Group Rules
- Group names are 3-6 characters, must be unique
- One group per player
- No member limit
- Invites expire after 120 seconds (configurable)
- Leave/Disband both require confirmation (30 second window)
- When the owner leaves, ownership transfers to the oldest member
- If the last member leaves, the group is disbanded
- Group stats only count activity after joining (not lifetime stats)

## Group Configuration

```xml
<MaxGroupSize>10</MaxGroupSize>
<InviteTimeoutSeconds>120</InviteTimeoutSeconds>
<GroupNameMinLength>3</GroupNameMinLength>
<GroupNameMaxLength>6</GroupNameMaxLength>
<GroupLeaderboardPageSize>10</GroupLeaderboardPageSize>
```

| Option | Default | Description |
|--------|---------|-------------|
| `MaxGroupSize` | 10 | Maximum members per group (set to a high value for unlimited) |
| `InviteTimeoutSeconds` | 120 | How long group invites last |
| `GroupNameMinLength` | 3 | Minimum characters for group names |
| `GroupNameMaxLength` | 6 | Maximum characters for group names |
| `GroupLeaderboardPageSize` | 10 | Groups per page on leaderboard |

## Group Data Storage

Groups are stored in `Plugins/PlayerStats/Groups.json` (JSON) or `playerstats_groups` table (MySQL). Each player's `GroupId` is stored in their PlayerStats record.

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
The default configuration automatically bans players who achieve more than 30 kills with over 80% headshot accuracy within their first hour of playtime:

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

### Other Options

| Option | Description |
|--------|-------------|
| `DatabaseProvider` | Choose between `json` or `mysql` for data storage |
| `SaveIntervalSeconds` | Interval in seconds for saving player stats from memory to the database |
| `ShowCombinedDeaths` | When enabled, deaths in statistics count both PVE and PVP deaths. When disabled, only deaths matching the current `StatsMode` are counted |
| `EnableUIEffect` | Activates the optional UI for viewing statistics. Install the workshop mod to use it |
| `ShowUIEffectByDefault` | Whether the UI should be shown by default when players join. Players can toggle it using `/statsui` command |
| `EnableJoinLeaveMessages` | Displays messages when players join or leave the server (with their rank if they reached a minimum ranking threshold) |
| `MinimumRankingTreshold` | Minimum number of kills required to appear on the ranking |
| `EnableRewards` | Activates the permission-based rewards system |

---

## Commands

### Stats Commands

| Command | Description |
|---------|-------------|
| `/playtime [player]` | Shows your or another player's total playtime |
| `/stats [player]` | Displays your or another player's stats |
| `/rank [player]` | Shows your or another player's ranking |
| `/sstats [player]` | Displays your or another player's session stats |
| `/splaytime [player]` | Shows your or another player's session playtime |
| `/ranking` | Displays the top players ranking |
| `/statsui` | Toggles the stats UI on/off |

### Group Commands

| Command | Description |
|---------|-------------|
| `/group` | Shows the group command menu in chat |
| `/groupui` | Toggles the group UI panel on/off |
| `/group create <name>` | Creates a new group (3-6 characters, unique) |
| `/group invite <player>` | Invites a player to your group (owner only, online only) |
| `/group join <groupName>` | Accepts a pending group invite |
| `/group leave` | Prompts confirmation to leave (`/group leave confirm`) |
| `/group info [groupName]` | Shows your group's info or any group's info |
| `/group leaderboard [page]` | Displays the group leaderboard |
| `/group disband` | Prompts confirmation to disband (`/group disband confirm`) |
| `/group kick <player>` | Kicks a member from your group (owner only) |
| `/group transfer <player>` | Transfers group ownership to a member |
| `/group stats <player>` | Shows the group stats of another player |
| `/groupcommands` or `/gcmds` | Shows all group commands in chat |

---

## Permissions

```xml
<!-- Stats Permissions -->
<Permission Cooldown="0">playtime</Permission>
<Permission Cooldown="0">stats</Permission>
<Permission Cooldown="0">rank</Permission>
<Permission Cooldown="0">sstats</Permission>
<Permission Cooldown="0">splaytime</Permission>
<Permission Cooldown="0">ranking</Permission>
<Permission Cooldown="0">statsui</Permission>

<!-- Group Permissions -->
<Permission Cooldown="0">group</Permission>
<Permission Cooldown="0">groupui</Permission>
<Permission Cooldown="0">groupcommands</Permission>
<Permission Cooldown="0">gcmds</Permission>
```

---

## Full Configuration (Fresh Install)

This is the complete default configuration including group settings:

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
  <EnableRewards>true</EnableRewards>
  <Rewards>
    <Reward Name="VIP Rank" Treshold="50" PermissionGroup="vip" />
    <Reward Name="MVP Rank" Treshold="125" PermissionGroup="mvp" />
  </Rewards>
  <MaxGroupSize>10</MaxGroupSize>
  <InviteTimeoutSeconds>120</InviteTimeoutSeconds>
  <GroupNameMinLength>3</GroupNameMinLength>
  <GroupNameMaxLength>6</GroupNameMaxLength>
  <GroupLeaderboardPageSize>10</GroupLeaderboardPageSize>
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

# Upgrading from PlayerStats v1.2.0 to PlayerStats & Group System

If you already have PlayerStats and only want to add the group features, follow these steps:

## Step 1: Replace the DLL

1. Stop your server
2. Replace `Rocket/Plugins/PlayerStats/PlayerStats.dll` with the new one from this release
3. The `Libraries/` folder remains unchanged (same dependencies)

## Step 2: Add Group Configuration

Add these 5 lines **inside** your existing `<PlayerStatsConfiguration>` block:

```xml
<MaxGroupSize>10</MaxGroupSize>
<InviteTimeoutSeconds>120</InviteTimeoutSeconds>
<GroupNameMinLength>3</GroupNameMinLength>
<GroupNameMaxLength>6</GroupNameMaxLength>
<GroupLeaderboardPageSize>10</GroupLeaderboardPageSize>
```

Add them anywhere before the closing `</PlayerStatsConfiguration>` tag.

## Step 3: Add Group Permissions

Add these to your `Permissions.config.xml` default group:

```xml
<Permission Cooldown="0">group</Permission>
<Permission Cooldown="0">groupui</Permission>
<Permission Cooldown="0">groupcommands</Permission>
<Permission Cooldown="0">gcmds</Permission>
```

## Step 4: Add Group Commands to Commands.config.xml

Add these lines inside the `<Commands>` block:

```xml
<Command Name="groupui" Enabled="true" Priority="Normal">RestoreMonarchy.PlayerStats.Commands.GroupUICommand/groupui</Command>
<Command Name="groupcommands" Enabled="true" Priority="Normal">RestoreMonarchy.PlayerStats.Commands.GroupCommandsCommand/groupcommands</Command>
<Command Name="gcmds" Enabled="true" Priority="Normal">RestoreMonarchy.PlayerStats.Commands.GroupCommandsCommand/gcmds</Command>
```

## Step 5: Start Server

Start your server. Existing player data and stats are preserved.

> ⚠️ **Important**: If you were testing the group system previously, delete `Plugins/PlayerStats/Groups.json` to start fresh. Group stats only count from the moment a player joins a group.

---

## Installation (New Server)

1. Copy `Libraries/*` into `Rocket/Libraries/`
2. Copy `Plugins/PlayerStats.dll` into `Rocket/Plugins/PlayerStats/`
3. Start the server — configuration files will be auto-generated
4. Stop the server, configure as needed, restart

---

## Credits
UI made by **💪 Soer (Unbeaten)**. He also sponsored the creation of this plugin 💸. 

Group system extension developed by **Kaiser-Fahim**.
---

## Frequently Asked Questions

1. **How do I enable the UI?**  
   Set `<EnableUIEffect>true</EnableUIEffect>` in the configuration and subscribe to the workshop item.

2. **Can I use MySQL instead of JSON?**  
   Yes, change `<DatabaseProvider>json</DatabaseProvider>` to `<DatabaseProvider>mysql</DatabaseProvider>` and configure your connection string.

3. **How do I customize rewards?**  
   Edit the `<Rewards>` section in the configuration file with your desired thresholds and permission groups.

4. **How can I create a custom UI?**  
   You can download the `statsui.unitypackage` from the **All versions** page and import it into your Unity project. Then, customize the UI as needed and reupload it to the workshop. Remember in Unity do not rename any of the objects in the hierarchy, as the plugin uses the object names to find them.

5. **Why are group stats at 0?**  
   Group stats only count kills and deaths that happen **after** a player joins the group. Lifetime stats are not included.

6. **Can I increase the group name character limit?**  
   Yes, change `<GroupNameMaxLength>6</GroupNameMaxLength>` to your desired value.

7. **Do group invites work for offline players?**  
   No, invites are online only. The invited player must be on the server.

8. **What happens when the group owner leaves?**  
   Ownership automatically transfers to the oldest remaining member. If no members remain, the group is disbanded.

---

*For support, bug reports, or feature requests, please write on our forum or join our Discord.*
