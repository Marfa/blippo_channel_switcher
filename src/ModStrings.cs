namespace BlippoChannelHopper;

internal enum UiLanguage
{
    Auto = 0,
    Russian = 1,
    English = 2,
}

internal static class ModStrings
{
    public static bool IsRussian { get; private set; }

    public static void Apply(UiLanguage language)
    {
        IsRussian = language switch
        {
            UiLanguage.Russian => true,
            UiLanguage.English => false,
            _ => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                .Equals("ru", System.StringComparison.OrdinalIgnoreCase),
        };
    }

    public static string WindowTitle => IsRussian ? "Blippo Channel Hopper" : "Blippo Channel Hopper";

    public static string TimerHop => IsRussian ? "Переключение по таймеру" : "Timer hop";
    public static string EndHop => IsRussian ? "Автопереключение" : "Auto hop (end of loop)";
    public static string SkipUnwanted => IsRussian ? "Пропуск снега и помех" : "Skip snow and interference";
    public static string DisableSignalLoss => IsRussian ? "Отключить деградацию сигнала" : "Disable signal loss";

    public static string Interval(float minutes) =>
        IsRussian ? $"Интервал: {minutes:0.#} мин" : $"Interval: {minutes:0.#} min";

    public static string MinusOneMin => IsRussian ? "−1 мин" : "-1 min";
    public static string PlusOneMin => IsRussian ? "+1 мин" : "+1 min";

    public static string Hotkeys => IsRussian
        ? "F9 таймер | F10 настройки | [ ] интервал"
        : "F9 toggle timer | F10 settings | [ ] interval";

    public static string Close => IsRussian ? "Закрыть" : "Close";

    public static string LanguageLabel => IsRussian ? "Язык" : "Language";

    public static string LanguageName(UiLanguage language) => language switch
    {
        UiLanguage.Auto => IsRussian ? "Авто" : "Auto",
        UiLanguage.Russian => "Русский",
        UiLanguage.English => "English",
        _ => "Auto",
    };

    public static string ChannelUnknown => IsRussian ? "— > — > —" : "— > — > —";

    public static string FormatChannelLabel(ChannelObject channel) =>
        $"{channel.channelNumber:00} {channel.callSign}";

    public static string ChannelStrip(string previous, string current, string next, float? hopMinutes) =>
        hopMinutes.HasValue
            ? (IsRussian
                ? $"{previous} > {current} > {next}\nПереключение через: {hopMinutes.Value:0.#} мин"
                : $"{previous} > {current} > {next}\nSwitch in: {hopMinutes.Value:0.#} min")
            : $"{previous} > {current} > {next}";

    public static string AllOff => IsRussian ? "Все режимы выключены." : "All features are off.";

    public static string SignalLossClear(float seconds) =>
        IsRussian
            ? $"Сброс деградации через: {seconds:0} с"
            : $"Signal loss clear in: {seconds:0} s";

    public static string SkipHopping => IsRussian ? "Пропуск: переключаю…" : "Skip unwanted: hopping...";
    public static string SkipOn => IsRussian ? "Пропуск: вкл" : "Skip unwanted: on";

    public static string WaitingBroadcast => IsRussian
        ? "Ожидание экрана трансляции…"
        : "Waiting for TV broadcast screen...";

    public static string NextTimerHop(float minutes) =>
        IsRussian
            ? $"Следующее переключение через: {minutes:0.#} мин"
            : $"Next timer hop in: {minutes:0.#} min";

    public static string EndHopWaiting(float loopMinutesRemaining) =>
        IsRussian
            ? $"Автопереключение: цикл через {loopMinutesRemaining:0.#} мин"
            : $"End hop: loop ends in {loopMinutesRemaining:0.#} min";
}
