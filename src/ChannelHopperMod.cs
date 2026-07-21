using System.Reflection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BlippoChannelHopper.ChannelHopperMod), "Blippo Channel Hopper", "1.2.0", "Marfa")]

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
    private const double LoopWrapToleranceSeconds = 1.0;

    private MelonPreferences_Category _prefs = null!;
    private MelonPreferences_Entry<bool> _timerEnabled = null!;
    private MelonPreferences_Entry<bool> _endHopEnabled = null!;
    private MelonPreferences_Entry<bool> _skipSnowEnabled = null!;
    private MelonPreferences_Entry<bool> _disableSignalLossEnabled = null!;
    private MelonPreferences_Entry<float> _intervalMinutes = null!;
    private MelonPreferences_Entry<int> _uiLanguage = null!;

    private float _timer;
    private float _signalLossDisableTimer;
    private double _trackedLoopTime = double.NaN;
    private bool _runtimeTimerEnabled;
    private bool _runtimeEndHopEnabled;
    private bool _runtimeSkipSnowEnabled;
    private bool _runtimeDisableSignalLossEnabled;
    private bool _showSettings;
    private Rect _windowRect = new Rect(24f, 24f, 420f, 420f);

    private static FieldInfo? _channelsField;
    private static MethodInfo? _getChannelIndexMethod;
    private static MethodInfo? _getCurrentEpisodeMethod;

    private float IntervalSeconds => _intervalMinutes.Value * SecondsPerMinute;

    private UiLanguage CurrentUiLanguage =>
        (UiLanguage)Mathf.Clamp(_uiLanguage.Value, (int)UiLanguage.Auto, (int)UiLanguage.English);

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
            "Hop on loop end",
            "Switch channel when the full 5-timeslot broadcast loop completes");
        _skipSnowEnabled = _prefs.CreateEntry(
            "SkipSnow",
            false,
            "Skip snow",
            "Skip snow, weak-signal tips, locked access, Femtofax, and credits channels");
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
        _uiLanguage = _prefs.CreateEntry(
            "UiLanguage",
            (int)UiLanguage.Auto,
            "UI language",
            "0 = Auto (system), 1 = Russian, 2 = English");

        _runtimeTimerEnabled = _timerEnabled.Value;
        _runtimeEndHopEnabled = _endHopEnabled.Value;
        if (_runtimeTimerEnabled && _runtimeEndHopEnabled)
        {
            _runtimeEndHopEnabled = false;
            _endHopEnabled.Value = false;
        }

        _runtimeSkipSnowEnabled = _skipSnowEnabled.Value;
        _runtimeDisableSignalLossEnabled = _disableSignalLossEnabled.Value;
        _timer = 0f;
        _signalLossDisableTimer = 0f;
        ModStrings.Apply(CurrentUiLanguage);

        MelonLogger.Msg("Loaded. F9 = toggle timer, F10 = settings, [ / ] = interval -/+ 1 min");
        LogState();
    }

    public override void OnGUI()
    {
        if (!_showSettings)
        {
            return;
        }

        _windowRect = GUILayout.Window(0xB11CC0, _windowRect, DrawSettingsWindow, ModStrings.WindowTitle);
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

        // Skip must run on FEMTOFAX too (channel 21), before the broadcast-only guards.
        if (_runtimeSkipSnowEnabled)
        {
            TrySkipUnwantedChannel();
        }

        // Track loop time even when channel hops are temporarily blocked (menus, overlays).
        if (_runtimeEndHopEnabled)
        {
            TryHopOnLoopEnd();
        }

        if (!CanHopChannel())
        {
            if (_runtimeTimerEnabled)
            {
                return;
            }
        }
        else if (_runtimeTimerEnabled)
        {
            _timer += Time.deltaTime;
            if (_timer >= IntervalSeconds)
            {
                _timer = 0f;
                HopChannelDown();
            }
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

    private static bool CanRunSkipCheck()
    {
        if (Utilities.NeedToCancelInput())
        {
            return false;
        }

        SystemScreen.Type screen = GameManager.currentSystemScreen;
        return screen == SystemScreen.Type.BROADCAST_DISPLAY
            || screen == SystemScreen.Type.FEMTOFAX;
    }

    private static bool ShouldSkipCurrentChannel()
    {
        ChannelObject channel = GameManager.instance.currentlyTunedChannel;
        return channel != null && ShouldSkipChannel(channel);
    }

    private static bool ShouldSkipChannel(ChannelObject channel)
    {
        if (channel.id == "fax" || channel.id == "blip")
        {
            return true;
        }

        if (GameManager.currentSystemScreen != SystemScreen.Type.BROADCAST_DISPLAY)
        {
            return false;
        }

        EpisodeObject? episode = GetCurrentEpisodeForChannel(channel);
        if (episode == null)
        {
            return false;
        }

        if (episode.snowEpisodeObject != null || episode.nonVideoEpisode)
        {
            return true;
        }

        // ACC locked slots: AUX/LOCAL/GLOBAL/UNIVERSAL ACCESS ("CHANNEL LOCKED FOR … ACCESS").
        ShowObject show = episode.show;
        return show != null
            && !string.IsNullOrEmpty(show.name)
            && show.name.IndexOf("Access", System.StringComparison.OrdinalIgnoreCase) >= 0;
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

    /// <summary>
    /// Hop when the full broadcast loop completes (~5 min). Uses currentTime wrap, not per-slot changes.
    /// </summary>
    private void TryHopOnLoopEnd()
    {
        double loopTime = GameManager.currentTime;

        if (double.IsNaN(_trackedLoopTime))
        {
            _trackedLoopTime = loopTime;
            return;
        }

        bool loopWrapped = loopTime + LoopWrapToleranceSeconds < _trackedLoopTime;
        _trackedLoopTime = loopTime;

        if (!loopWrapped || !CanHopChannel())
        {
            return;
        }

        HopChannelDown();
    }

    private void TrySkipUnwantedChannel()
    {
        if (!CanRunSkipCheck() || !ShouldSkipCurrentChannel())
        {
            return;
        }

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
        MelonLogger.Msg(ModStrings.Interval(_intervalMinutes.Value));
    }

    private void DrawSettingsWindow(int id)
    {
        DrawChannelLabel();
        DrawLanguageSelector();

        bool newTimerEnabled = GUILayout.Toggle(_runtimeTimerEnabled, ModStrings.TimerHop);
        if (newTimerEnabled != _runtimeTimerEnabled)
        {
            SetTimerEnabled(newTimerEnabled);
        }

        bool newEndHopEnabled = GUILayout.Toggle(_runtimeEndHopEnabled, ModStrings.EndHop);
        if (newEndHopEnabled != _runtimeEndHopEnabled)
        {
            SetEndHopEnabled(newEndHopEnabled);
        }

        bool newSkipSnowEnabled = GUILayout.Toggle(_runtimeSkipSnowEnabled, ModStrings.SkipUnwanted);
        if (newSkipSnowEnabled != _runtimeSkipSnowEnabled)
        {
            SetSkipSnowEnabled(newSkipSnowEnabled);
        }

        bool newDisableSignalLoss = GUILayout.Toggle(
            _runtimeDisableSignalLossEnabled,
            ModStrings.DisableSignalLoss);
        if (newDisableSignalLoss != _runtimeDisableSignalLossEnabled)
        {
            SetDisableSignalLossEnabled(newDisableSignalLoss);
        }

        GUILayout.Space(6f);
        GUILayout.Label(ModStrings.Interval(_intervalMinutes.Value));
        float newInterval = GUILayout.HorizontalSlider(
            _intervalMinutes.Value,
            MinIntervalMinutes,
            MaxIntervalMinutes);
        if (!Mathf.Approximately(newInterval, _intervalMinutes.Value))
        {
            SetInterval(newInterval);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(ModStrings.MinusOneMin))
        {
            AdjustInterval(-1f);
        }

        if (GUILayout.Button(ModStrings.PlusOneMin))
        {
            AdjustInterval(1f);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        DrawStatusLabel();

        GUILayout.Space(6f);
        GUILayout.Label(ModStrings.Hotkeys);

        if (GUILayout.Button(ModStrings.Close))
        {
            _showSettings = false;
        }

        GUI.DragWindow();
    }

    private void DrawChannelLabel()
    {
        if (GameManager.instance == null || GameManager.instance.currentlyTunedChannel == null)
        {
            GUILayout.Label(ModStrings.ChannelUnknown);
            return;
        }

        ChannelObject current = GameManager.instance.currentlyTunedChannel;
        string previous = FormatChannelLabel(GetAdjacentChannel(1));
        string next = FormatChannelLabel(GetPredictedNextHopChannel());
        string currentLabel = ModStrings.FormatChannelLabel(current);
        float? hopMinutes = GetNextHopMinutes();

        GUILayout.Label(ModStrings.ChannelStrip(previous, currentLabel, next, hopMinutes));
    }

    private static string FormatChannelLabel(ChannelObject? channel) =>
        channel == null ? "—" : ModStrings.FormatChannelLabel(channel);

    private float? GetNextHopMinutes()
    {
        if (_runtimeEndHopEnabled)
        {
            return Mathf.Max(0f, Config.instance.loopLength - (float)GameManager.currentTime) / SecondsPerMinute;
        }

        if (_runtimeTimerEnabled)
        {
            return Mathf.Max(0f, IntervalSeconds - _timer) / SecondsPerMinute;
        }

        return null;
    }

    /// <summary>
    /// Next channel the mod will land on (channel-down, skipping unwanted channels when enabled).
    /// </summary>
    private ChannelObject? GetPredictedNextHopChannel()
    {
        ChannelObject? candidate = GetAdjacentChannel(-1);
        if (!_runtimeSkipSnowEnabled || candidate == null)
        {
            return candidate;
        }

        EnsureChannelReflection();
        if (_channelsField?.GetValue(GameManager.instance) is not System.Collections.IList channelList)
        {
            return candidate;
        }

        int safety = 0;
        while (candidate != null && ShouldSkipChannel(candidate) && safety < channelList.Count)
        {
            candidate = GetAdjacentChannel(candidate, -1);
            safety++;
        }

        return candidate;
    }

    private static ChannelObject? GetAdjacentChannel(int offset) =>
        GetAdjacentChannel(GameManager.instance?.currentlyTunedChannel, offset);

    private static ChannelObject? GetAdjacentChannel(ChannelObject? fromChannel, int offset)
    {
        GameManager gameManager = GameManager.instance;
        if (gameManager == null || fromChannel == null)
        {
            return null;
        }

        EnsureChannelReflection();
        if (_channelsField?.GetValue(gameManager) is not System.Collections.IList channelList || channelList.Count == 0)
        {
            return null;
        }

        int index = InvokeGetChannelIndex(fromChannel, offset);
        if (index < 0 || index >= channelList.Count)
        {
            return null;
        }

        return channelList[index] is Channel channel ? channel.channelObject : null;
    }

    private static EpisodeObject? GetCurrentEpisodeForChannel(ChannelObject channel)
    {
        EnsureChannelReflection();
        if (_getCurrentEpisodeMethod == null)
        {
            return null;
        }

        return _getCurrentEpisodeMethod.Invoke(GameManager.instance, new object[] { channel }) as EpisodeObject;
    }

    private static int InvokeGetChannelIndex(ChannelObject channel, int offset)
    {
        EnsureChannelReflection();
        if (_getChannelIndexMethod == null)
        {
            GameManager gameManager = GameManager.instance;
            if (_channelsField?.GetValue(gameManager) is not System.Collections.IList channelList || channelList.Count == 0)
            {
                return 0;
            }

            int baseIndex = 0;
            for (int i = 0; i < channelList.Count; i++)
            {
                if (channelList[i] is Channel listedChannel && listedChannel.channelObject == channel)
                {
                    baseIndex = i;
                    break;
                }
            }

            int index = baseIndex + offset;
            if (index >= channelList.Count)
            {
                index -= channelList.Count;
            }
            else if (index < 0)
            {
                index += channelList.Count;
            }

            return index;
        }

        return (int)_getChannelIndexMethod.Invoke(
            GameManager.instance,
            new object[] { channel, offset })!;
    }

    private static void EnsureChannelReflection()
    {
        if (_channelsField != null && _getChannelIndexMethod != null && _getCurrentEpisodeMethod != null)
        {
            return;
        }

        const BindingFlags instance = BindingFlags.NonPublic | BindingFlags.Instance;
        _channelsField ??= typeof(GameManager).GetField("channels", instance);
        _getChannelIndexMethod ??= typeof(GameManager).GetMethod(
            "GetChannelIndex",
            instance,
            binder: null,
            types: new[] { typeof(ChannelObject), typeof(int) },
            modifiers: null);
        _getCurrentEpisodeMethod ??= typeof(GameManager).GetMethod(
            "GetCurrentEpisode",
            instance,
            binder: null,
            types: new[] { typeof(ChannelObject) },
            modifiers: null);
    }

    private void DrawLanguageSelector()
    {
        GUILayout.Label(ModStrings.LanguageLabel);
        GUILayout.BeginHorizontal();
        DrawLanguageButton(UiLanguage.Auto);
        DrawLanguageButton(UiLanguage.Russian);
        DrawLanguageButton(UiLanguage.English);
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
    }

    private void DrawLanguageButton(UiLanguage language)
    {
        bool selected = CurrentUiLanguage == language;
        GUI.enabled = !selected;
        if (GUILayout.Button(ModStrings.LanguageName(language)) && !selected)
        {
            SetUiLanguage(language);
        }

        GUI.enabled = true;
    }

    private void DrawStatusLabel()
    {
        if (!AnyFeatureEnabled)
        {
            GUILayout.Label(ModStrings.AllOff);
            return;
        }

        if (_runtimeDisableSignalLossEnabled)
        {
            float remaining = Mathf.Max(0f, SignalLossDisableIntervalSeconds - _signalLossDisableTimer);
            GUILayout.Label(ModStrings.SignalLossClear(remaining));
        }

        if (!AnyHopFeatureEnabled)
        {
            return;
        }

        if (_runtimeSkipSnowEnabled && CanRunSkipCheck())
        {
            GUILayout.Label(ShouldSkipCurrentChannel() ? ModStrings.SkipHopping : ModStrings.SkipOn);
        }

        if (_runtimeTimerEnabled)
        {
            float remaining = Mathf.Max(0f, IntervalSeconds - _timer) / SecondsPerMinute;
            GUILayout.Label(ModStrings.NextTimerHop(remaining));
        }

        if (_runtimeEndHopEnabled)
        {
            float remaining = Mathf.Max(0f, Config.instance.loopLength - (float)GameManager.currentTime) / SecondsPerMinute;
            GUILayout.Label(ModStrings.EndHopWaiting(remaining));
        }

        if (!CanHopChannel())
        {
            GUILayout.Label(ModStrings.WaitingBroadcast);
        }
    }

    private void SetTimerEnabled(bool enabled)
    {
        if (enabled && _runtimeEndHopEnabled)
        {
            _runtimeEndHopEnabled = false;
            _endHopEnabled.Value = false;
            _trackedLoopTime = double.NaN;
        }

        _runtimeTimerEnabled = enabled;
        _timerEnabled.Value = enabled;
        MelonPreferences.Save();
        if (enabled)
        {
            _timer = 0f;
        }

        LogState();
    }

    private void SetEndHopEnabled(bool enabled)
    {
        if (enabled && _runtimeTimerEnabled)
        {
            _runtimeTimerEnabled = false;
            _timerEnabled.Value = false;
        }

        _runtimeEndHopEnabled = enabled;
        _endHopEnabled.Value = enabled;
        MelonPreferences.Save();
        _trackedLoopTime = double.NaN;
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

    private void SetUiLanguage(UiLanguage language)
    {
        _uiLanguage.Value = (int)language;
        MelonPreferences.Save();
        ModStrings.Apply(language);
    }

    private void SetInterval(float minutes)
    {
        _intervalMinutes.Value = Mathf.Clamp(minutes, MinIntervalMinutes, MaxIntervalMinutes);
        MelonPreferences.Save();
        _timer = 0f;
    }

    private void LogState()
    {
        string timer = _runtimeTimerEnabled ? "ON" : "OFF";
        string endHop = _runtimeEndHopEnabled ? "ON" : "OFF";
        string skipSnow = _runtimeSkipSnowEnabled ? "ON" : "OFF";
        string signalLoss = _runtimeDisableSignalLossEnabled ? "ON" : "OFF";
        MelonLogger.Msg(
            $"Timer hop {timer} | end hop {endHop} | skip unwanted {skipSnow} | disable signal loss {signalLoss} | {ModStrings.Interval(_intervalMinutes.Value)}");
    }
}
