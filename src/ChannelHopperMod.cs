using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BlippoChannelHopper.ChannelHopperMod), "Blippo Channel Hopper", "1.0.0", "Marfa")]
[assembly: MelonGame("Noble Robot", "Blippo+")]

namespace BlippoChannelHopper;

/// <summary>
/// Periodically switches to the next channel while watching broadcast TV.
/// </summary>
public sealed class ChannelHopperMod : MelonMod
{
    private const float MinIntervalMinutes = 1f;
    private const float MaxIntervalMinutes = 60f;
    private const float DefaultIntervalMinutes = 1f;
    private const float SecondsPerMinute = 60f;

    private MelonPreferences_Category _prefs = null!;
    private MelonPreferences_Entry<bool> _enabled = null!;
    private MelonPreferences_Entry<float> _intervalMinutes = null!;

    private float _timer;
    private bool _runtimeEnabled;
    private bool _showSettings;
    private Rect _windowRect = new Rect(24f, 24f, 340f, 220f);

    private float IntervalSeconds => _intervalMinutes.Value * SecondsPerMinute;

    public override void OnInitializeMelon()
    {
        _prefs = MelonPreferences.CreateCategory("BlippoChannelHopper", "Blippo Channel Hopper");
        _enabled = _prefs.CreateEntry("Enabled", false, "Auto channel hop", "Start with auto hopping enabled");
        _intervalMinutes = _prefs.CreateEntry(
            "IntervalMinutes",
            DefaultIntervalMinutes,
            "Interval (minutes)",
            "Delay between channel-down actions");

        _runtimeEnabled = _enabled.Value;
        _timer = 0f;

        MelonLogger.Msg("Loaded. F9 = toggle, F10 = settings, [ / ] = interval -/+ 1 min");
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
            SetEnabled(!_runtimeEnabled);
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            AdjustInterval(-1f);
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            AdjustInterval(1f);
        }

        if (!_runtimeEnabled || GameManager.instance == null)
        {
            return;
        }

        if (!CanHopChannel())
        {
            _timer = 0f;
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < IntervalSeconds)
        {
            return;
        }

        _timer = 0f;
        GameManager.instance.broadcastDisplay.ChannelDown();
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
        GUILayout.Label($"Status: {(_runtimeEnabled ? "ON" : "OFF")}");

        bool newEnabled = GUILayout.Toggle(_runtimeEnabled, "Auto channel hop");
        if (newEnabled != _runtimeEnabled)
        {
            SetEnabled(newEnabled);
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
        if (_runtimeEnabled && CanHopChannel())
        {
            GUILayout.Label($"Next hop in: {FormatMinutes(Mathf.Max(0f, IntervalSeconds - _timer) / SecondsPerMinute)}");
        }
        else if (_runtimeEnabled)
        {
            GUILayout.Label("Waiting for TV broadcast screen...");
        }
        else
        {
            GUILayout.Label("Auto hop is off.");
        }

        GUILayout.Space(6f);
        GUILayout.Label("F9 toggle | F10 settings | [ ] interval");

        if (GUILayout.Button("Close"))
        {
            _showSettings = false;
        }

        GUI.DragWindow();
    }

    private void SetEnabled(bool enabled)
    {
        _runtimeEnabled = enabled;
        _enabled.Value = enabled;
        MelonPreferences.Save();
        _timer = 0f;
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
        string state = _runtimeEnabled ? "ON" : "OFF";
        MelonLogger.Msg($"Auto hop {state} | interval {FormatMinutes(_intervalMinutes.Value)}");
    }
}
