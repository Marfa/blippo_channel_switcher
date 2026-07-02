# **Auto channel surfing for Blippo+ — on a timer, no remote**

A [MelonLoader](https://github.com/LavaGang/MelonLoader) mod for [Blippo+](https://store.steampowered.com/app/3323850/Blippo/) that flips channels down while you watch live TV. Interval is in minutes; settings live in an in-game window.

```text
F10 → settings → enable → set interval → watch TV
```

| Feature | What it does |
| --- | --- |
| Auto hop | Every N minutes, switches channel down on the broadcast screen |
| Settings window (F10) | On/off, 1–60 min slider, countdown to next switch |
| Hotkeys | F9 — quick toggle, `[` / `]` — interval ±1 min |
| Persistence | Settings saved to `UserData/MelonPreferences.cfg` |
| Safety | Does not run in menus, the program guide, or modal dialogs |

> PC (Steam) only. Requires Blippo+ and MelonLoader.

[Русская версия / Russian README](README.md)

---

## Quick Start

### 1. Install the game

Buy and install **Blippo+** on Steam. Launch once, then quit.

### 2. Install MelonLoader

1. Download [MelonLoader.Installer.exe](https://github.com/LavaGang/MelonLoader/releases/latest).
2. Run the installer.
3. Select `Blippo+.exe` in the game folder:

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\Blippo+.exe
```

4. Click **Install** and wait until it finishes.

### 3. Install the mod

**Option A — from a release (easiest)**

1. Open [Releases](https://github.com/Marfa/blippo_channel_switcher/releases).
2. Download `BlippoChannelHopper.dll` from **v1.0.0**.
3. Drop the file into the game's `Mods` folder:

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\Mods\BlippoChannelHopper.dll
```

Create the `Mods` folder if it does not exist.

**Option B — build from source**

```powershell
git clone https://github.com/Marfa/blippo_channel_switcher.git
cd blippo_channel_switcher
dotnet build BlippoChannelHopper.csproj -c Release
```

Copy `bin\BlippoChannelHopper.dll` into the game's `Mods` folder.

### 4. Play

1. Launch Blippo+ through Steam.
2. Get to the **TV broadcast** screen.
3. Press **F10** to open the mod window.
4. Enable **Auto channel hop** and set the interval in minutes.

---

## Controls

| Key | Action |
| --- | --- |
| **F10** | Settings window |
| **F9** | Toggle auto hop on/off |
| **`[`** | Interval −1 min |
| **`]`** | Interval +1 min |

In the settings window: checkbox, 1–60 min slider, **-1 min** / **+1 min** buttons, countdown to the next channel.

---

## Where settings are stored

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\UserData\MelonPreferences.cfg
```

Example:

```toml
[BlippoChannelHopper]
Enabled = false
IntervalMinutes = 1.0
```

You do not need to edit this file manually — use **F10** in-game.

---

## Building

Requirements: [.NET SDK](https://dotnet.microsoft.com/download) 6.0+.

By default the project points at the standard Steam install path. For a custom path:

```powershell
dotnet build BlippoChannelHopper.csproj -c Release /p:BlippoGameDir="D:\Games\Blippo+"
```

The build needs DLLs from the installed game (`Blippo+_Data\Managed`) and MelonLoader in the game folder (`MelonLoader\net35\MelonLoader.dll`). Or extract [MelonLoader.x64.zip](https://github.com/LavaGang/MelonLoader/releases/latest) and pass:

```powershell
dotnet build -c Release /p:MelonLoaderDir="path\to\MelonLoader\MelonLoader"
```

---

## Limitations

- Works only on the **broadcast** screen (not EPG or menus).
- Unofficial mod — not supported by Panic or Steam Workshop.
- MelonLoader is third-party software; use at your own risk.

---

## Author

Code prepared with [Cursor](https://cursor.com).

Support the project:
- [DonationAlerts](https://www.donationalerts.com/r/themarfa)
- [Crypto donation via NOWPayments](https://nowpayments.io/donation/themarfa)
