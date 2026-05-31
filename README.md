# Player Stats
> Track and display player PVP and PVE statistics with ranking, rewards and UI

---

## Overview
Player Stats is a plugin that tracks various player statistics and provides ranking for both PVP and PVE gameplay. The plugin supports permission-based rewards and includes an optional visual UI.

## Features
| Feature | Description |
|---------|-------------|
| 💾 Data Storage | Store player stats in a JSON file or MySQL database |
| 📈 Stats Display | Show player stats, playtime, ranking, and session stats via commands |
| 🏆 Rankings | Support for both PVP (Kills) and PVE (Zombie Kills) leaderboards |
| 🎁 Rewards | Permission group rewards for reaching specific kill thresholds |
| 🔄 Migration | Automatic migration from Arechi PlayerStats plugin |
| 🖥️ User Interface | Optional UI for viewing PVP/PVE stats |

## Workshop (Optional)
The UI is optional and provides a visual display for PVP stats. 

- **Workshop Item**: [Player Stats UI](https://steamcommunity.com/sharedfiles/filedetails/?id=3352126593)
- **ID**: `3352126593`

> 💡 **PRO TIP**  
> Remember to set `<EnableUIEffect>true</EnableUIEffect>` in the configuration file to activate the UI.

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

---

*For support, bug reports, or feature requests, please write on our forum or join our Discord.*