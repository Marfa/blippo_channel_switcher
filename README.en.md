# **Auto channel surfing for Blippo+ — timer, end of slot, no remote**

A [MelonLoader](https://github.com/LavaGang/MelonLoader) mod for [Blippo+](https://store.steampowered.com/app/3323850/Blippo/) that flips channels down while you watch live TV. Switch on a timer, at the end of a timeslot, skip snow channels, and periodically clear signal degradation. Settings live in an in-game window.

```text
F10 → settings → enable the modes you want → watch TV
```

| Feature | What it does |
| --- | --- |
| Timer hop | Every N minutes, switches channel down on the broadcast screen |
| Auto hop (end of loop) | Switches channel down after the full 5-show loop (~5 min), not after each show |
| Skip snow and interference | Snow (CMBR), weak-signal tips (MEGA), locked ACCESS (72), Femtofax (21), credits (25) |
| Disable signal loss | Every 120 s, forces `signalLossInProgress` to `false` |
| Settings window (F10) | Channel strip (prev > current > next), UI language, checkboxes, slider, countdown |
| Hotkeys | F9 — toggle timer hop, `[` / `]` — interval ±1 min |
| Persistence | Settings saved to `UserData/MelonPreferences.cfg` |
| Safety | Channel hopping does not run in menus, the program guide, or modal dialogs |

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
2. Download `BlippoChannelHopper.dll` from **v1.2.0**.
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
4. Enable the modes you want (for example **Timer hop**) and set the interval if needed.

---

## Controls

| Key | Action |
| --- | --- |
| **F10** | Settings window |
| **F9** | Toggle **timer hop** on/off |
| **`[`** | Interval −1 min |
| **`]`** | Interval +1 min |

In the settings window:

- **channel strip**: `prev > current > next` and **“Switch in: N min”** (countdown for the active mode — timer *or* auto hop)
- **UI language**: Auto (system) / Русский / English
- **Timer hop** — every N minutes (cannot be enabled together with auto hop)
- **Auto hop** — after the full 5-show loop (~5 min)
- **Skip snow and interference** — CMBR, MEGA, locked ACCESS on 72, Femtofax (21), credits (25); “next” channel reflects the skip chain
- **Disable signal loss** — clear the degradation flag every 120 s
- 1–60 min slider, **−1 min** / **+1 min** buttons, status lines

**Timer hop** and **Auto hop** are mutually exclusive — enabling one disables the other. **Skip snow and interference** and **Disable signal loss** are independent and can combine with either hop mode.

The timer **does not reset** when you change channels manually with the remote.

---

## Where settings are stored

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\UserData\MelonPreferences.cfg
```

Example:

```toml
[BlippoChannelHopper]
Enabled = false
HopOnBroadcastEnd = false
SkipSnow = false
DisableSignalLoss = false
IntervalMinutes = 1.0
UiLanguage = 0
```

`UiLanguage`: `0` — auto, `1` — Russian, `2` — English.

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

After a successful build the DLL is copied into the game's `Mods` folder when that folder exists.

---

## Limitations

- Channel hopping works only on the **broadcast** screen (not EPG or menus).
- Disabling signal loss only sets `signalLossInProgress = false` every 120 s; already drifted tuner values are not reset.
- Unofficial mod — not supported by Panic or Steam Workshop.
- MelonLoader is third-party software; use at your own risk.

---

## Author

Code prepared with [Cursor](https://cursor.com).

Support the project:
- [DonationAlerts](https://www.donationalerts.com/r/themarfa)
- [Crypto donation via NOWPayments](https://nowpayments.io/donation/themarfa)

## License

**Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0)**

See [LICENSE](LICENSE) · https://creativecommons.org/licenses/by-nc-sa/4.0/
