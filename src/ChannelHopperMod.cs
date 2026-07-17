using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BlippoChannelHopper.ChannelHopperMod), "Blippo Channel Hopper", "1.1.0", "Marfa")]

[assembly: MelonGame("Noble Robot", "Blippo+")]

namespace BlippoChannelHopper;

/// <summary>
/// Switches channels while watching broadcast TV — by timer and/or when a timeslot ends.
/// </summary>
public sealed class ChannelHopperMod : MelonMod
{
    private const float MinIntervalMinutes = 1f;
    private const float MaxIntervalMinutes = 60f;
    private const float DefaultIntervalMinutes = 1f;
    private const float SecondsPerMinute = 60f;
    private const float SignalLossDisableIntervalSeconds = 120f;
    private const int UntrackedTimeSlot = int.MinValue;

    private MelonPreferences_Category _prefs = null!;
    private MelonPreferences_Entry<bool> _timerEnabled = null!;
    private MelonPreferences_Entry<bool> _endHopEnabled = null!;
    private MelonPreferences_Entry<bool> _skipSnowEnabled = null!;
    private MelonPreferences_Entry<bool> _disableSignalLossEnabled = null!;
    private MelonPreferences_Entry<float> _intervalMinutes = null!;

    private float _timer;
    private float _signalLossDisableTimer;
    private int _trackedTimeSlot = UntrackedTimeSlot;
    private bool _runtimeTimerEnabled;
    private bool _runtimeEndHopEnabled;
    private bool _runtimeSkipSnowEnabled;
    private bool _runtimeDisableSignalLossEnabled;
    private bool _showSettings;
    private Rect _windowRect = new Rect(24f, 24f, 360f, 320f);

    private float IntervalSeconds => _intervalMinutes.Value * SecondsPerMinute;

    private bool AnyHopFeatureEnabled =>
        _runtimeTimerEnabled || _runtimeEndHopEnabled || _runtimeSkipSnowEnabled;

    private bool AnyFeatureEnabled =>
        AnyHopFeatureEnabled || _runtimeDisableSignalLossEnabled;

    public override void OnInitializeMelon()
    {
        _prefs = MelonPreferences.CreateCategory("BlippoChannelHopper", "Blippo Channel Hopper");
        _timerEnabled = _prefs.CreateEntry(
            "Enabled",
            false,
            "Timer hop",
            "Switch channel on a timer");
        _endHopEnabled = _prefs.CreateEntry(
            "HopOnBroadcastEnd",
            false,
            "Hop on broadcast end",
            "Switch channel when the current timeslot ends");
        _skipSnowEnabled = _prefs.CreateEntry(
            "SkipSnow",
            false,
            "Skip snow",
            "Automatically skip channels with snowEpisodeObject");
        _disableSignalLossEnabled = _prefs.CreateEntry(
            "DisableSignalLoss",
            false,
            "Disable signal loss",
            "Force signalLossInProgress to false every 120 seconds");
        _intervalMinutes = _prefs.CreateEntry(
            "IntervalMinutes",
            DefaultIntervalMinutes,
            "Interval (minutes)",
            "Delay between channel-down actions for timer hop");

        _runtimeTimerEnabled = _timerEnabled.Value;
        _runtimeEndHopEnabled = _endHopEnabled.Value;
        _runtimeSkipSnowEnabled = _skipSnowEnabled.Value;
        _runtimeDisableSignalLossEnabled = _disableSignalLossEnabled.Value;
        _timer = 0f;
        _signalLossDisableTimer = 0f;

        MelonLogger.Msg("Loaded. F9 = toggle timer, F10 = settings, [ / ] = interval -/+ 1 min");
        LogState();
    }

    public override void OnGUI()
    {
        if (!_showSettings)
        {
            return;
        }

        _windowRect = GUILayout.Window(0xB11CC0, _windowRect, DrawSettingsWindow, "Blippo Channel Hopper");
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F10))
        {
            _showSettings = !_showSettings;
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            SetTimerEnabled(!_runtimeTimerEnabled);
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            AdjustInterval(-1f);
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            AdjustInterval(1f);
        }

        if (!AnyFeatureEnabled)
        {
            return;
        }

        if (_runtimeDisableSignalLossEnabled)
        {
            TryDisableSignalLoss();
        }

        if (!AnyHopFeatureEnabled || GameManager.instance == null)
        {
            return;
        }

        if (!CanHopChannel())
        {
            _timer = 0f;
            _trackedTimeSlot = UntrackedTimeSlot;
            return;
        }

        if (_runtimeEndHopEnabled)
        {
            TryHopOnTimeslotEnd();
        }

        if (_runtimeTimerEnabled)
        {
            _timer += Time.deltaTime;
            if (_timer >= IntervalSeconds)
            {
                _timer = 0f;
                HopChannelDown();
            }
        }

        if (_runtimeSkipSnowEnabled)
        {
            TrySkipSnow();
        }
    }

    /// <summary>
    /// Same guards the game uses before reading channel-change input.
    /// </summary>
    private static bool CanHopChannel()
    {
        if (Utilities.NeedToCancelInput())
        {
            return false;
        }

        if (GameManager.currentSystemScreen != SystemScreen.Type.BROADCAST_DISPLAY)
        {
            return false;
        }

        return GameManager.inWatchableState;
    }

    private static bool IsSnowEpisode()
    {
        EpisodeObject episode = GameManager.instance.broadcastDisplay.episodeObject;
        return episode != null && episode.snowEpisodeObject != null;
    }

    private void TryDisableSignalLoss()
    {
        _signalLossDisableTimer += Time.deltaTime;
        if (_signalLossDisableTimer < SignalLossDisableIntervalSeconds)
        {
            return;
        }

        _signalLossDisableTimer = 0f;
        ForceSignalLossOff();
    }

    private static void ForceSignalLossOff()
    {
        if (ViewerData_v1.current == null)
        {
            return;
        }

        ViewerData_v1.current.signalLossInProgress = false;
    }

    private void TryHopOnTimeslotEnd()
    {
        int slot = GameManager.currentTimeSlot;
        if (_trackedTimeSlot == UntrackedTimeSlot)
        {
            _trackedTimeSlot = slot;
            return;
        }

        if (slot == _trackedTimeSlot)
        {
            return;
        }

        _trackedTimeSlot = slot;
        _timer = 0f;
        HopChannelDown();
    }

    private void TrySkipSnow()
    {
        if (!IsSnowEpisode())
        {
            return;
        }

        _timer = 0f;
        HopChannelDown();
    }

    private static void HopChannelDown()
    {
        GameManager.instance.broadcastDisplay.ChannelDown();
    }

    private void AdjustInterval(float deltaMinutes)
    {
        float next = Mathf.Clamp(_intervalMinutes.Value + deltaMinutes, MinIntervalMinutes, MaxIntervalMinutes);
        if (Mathf.Approximately(next, _intervalMinutes.Value))
        {
            return;
        }

        SetInterval(next);
        MelonLogger.Msg($"Interval: {FormatMinutes(_intervalMinutes.Value)}");
    }

    private void DrawSettingsWindow(int id)
    {
        bool newTimerEnabled = GUILayout.Toggle(_runtimeTimerEnabled, "Переключение по таймеру");
        if (newTimerEnabled != _runtimeTimerEnabled)
        {
            SetTimerEnabled(newTimerEnabled);
        }

        bool newEndHopEnabled = GUILayout.Toggle(_runtimeEndHopEnabled, "Автопереключение");
        if (newEndHopEnabled != _runtimeEndHopEnabled)
        {
            SetEndHopEnabled(newEndHopEnabled);
        }

        bool newSkipSnowEnabled = GUILayout.Toggle(_runtimeSkipSnowEnabled, "Пропуск снега");
        if (newSkipSnowEnabled != _runtimeSkipSnowEnabled)
        {
            SetSkipSnowEnabled(newSkipSnowEnabled);
        }

        bool newDisableSignalLoss = GUILayout.Toggle(
            _runtimeDisableSignalLossEnabled,
            "Отключить деградацию сигнала");
        if (newDisableSignalLoss != _runtimeDisableSignalLossEnabled)
        {
            SetDisableSignalLossEnabled(newDisableSignalLoss);
        }

        GUILayout.Space(6f);
        GUILayout.Label($"Interval: {FormatMinutes(_intervalMinutes.Value)}");
        float newInterval = GUILayout.HorizontalSlider(
            _intervalMinutes.Value,
            MinIntervalMinutes,
            MaxIntervalMinutes);
        if (!Mathf.Approximately(newInterval, _intervalMinutes.Value))
        {
            SetInterval(newInterval);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-1 min"))
        {
            AdjustInterval(-1f);
        }

        if (GUILayout.Button("+1 min"))
        {
            AdjustInterval(1f);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        DrawStatusLabel();

        GUILayout.Space(6f);
        GUILayout.Label("F9 toggle timer | F10 settings | [ ] interval");

        if (GUILayout.Button("Close"))
        {
            _showSettings = false;
        }

        GUI.DragWindow();
    }

    private void DrawStatusLabel()
    {
        if (!AnyFeatureEnabled)
        {
            GUILayout.Label("All features are off.");
            return;
        }

        if (_runtimeDisableSignalLossEnabled)
        {
            float remaining = Mathf.Max(0f, SignalLossDisableIntervalSeconds - _signalLossDisableTimer);
            GUILayout.Label($"Signal loss clear in: {remaining:0} s");
        }

        if (!AnyHopFeatureEnabled)
        {
            return;
        }

        if (!CanHopChannel())
        {
            GUILayout.Label("Waiting for TV broadcast screen...");
            return;
        }

        if (_runtimeTimerEnabled)
        {
            GUILayout.Label(
                $"Next timer hop in: {FormatMinutes(Mathf.Max(0f, IntervalSeconds - _timer) / SecondsPerMinute)}");
        }

        if (_runtimeEndHopEnabled)
        {
            GUILayout.Label("End hop: waiting for current broadcast to finish...");
        }

        if (_runtimeSkipSnowEnabled)
        {
            GUILayout.Label(IsSnowEpisode() ? "Skip snow: hopping..." : "Skip snow: on");
        }
    }

    private void SetTimerEnabled(bool enabled)
    {
        _runtimeTimerEnabled = enabled;
        _timerEnabled.Value = enabled;
        MelonPreferences.Save();
        _timer = 0f;
        LogState();
    }

    private void SetEndHopEnabled(bool enabled)
    {
        _runtimeEndHopEnabled = enabled;
        _endHopEnabled.Value = enabled;
        MelonPreferences.Save();
        _trackedTimeSlot = UntrackedTimeSlot;
        LogState();
    }

    private void SetSkipSnowEnabled(bool enabled)
    {
        _runtimeSkipSnowEnabled = enabled;
        _skipSnowEnabled.Value = enabled;
        MelonPreferences.Save();
        LogState();
    }

    private void SetDisableSignalLossEnabled(bool enabled)
    {
        _runtimeDisableSignalLossEnabled = enabled;
        _disableSignalLossEnabled.Value = enabled;
        MelonPreferences.Save();
        _signalLossDisableTimer = 0f;
        if (enabled)
        {
            ForceSignalLossOff();
        }

        LogState();
    }

    private void SetInterval(float minutes)
    {
        _intervalMinutes.Value = Mathf.Clamp(minutes, MinIntervalMinutes, MaxIntervalMinutes);
        MelonPreferences.Save();
        _timer = 0f;
    }

    private static string FormatMinutes(float minutes)
    {
        return $"{minutes:0.#} min";
    }

    private void LogState()
    {
        string timer = _runtimeTimerEnabled ? "ON" : "OFF";
        string endHop = _runtimeEndHopEnabled ? "ON" : "OFF";
        string skipSnow = _runtimeSkipSnowEnabled ? "ON" : "OFF";
        string signalLoss = _runtimeDisableSignalLossEnabled ? "ON" : "OFF";
        MelonLogger.Msg(
            $"Timer hop {timer} | end hop {endHop} | skip snow {skipSnow} | disable signal loss {signalLoss} | interval {FormatMinutes(_intervalMinutes.Value)}");
    }
}
