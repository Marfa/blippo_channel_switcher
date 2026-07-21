# **Автопереключение каналов в Blippo+ — таймер, конец эфира, без пульта**

MelonLoader-мод для [Blippo+](https://store.steampowered.com/app/3323850/Blippo/): листает каналы вниз, пока вы смотрите эфир. Можно переключать по таймеру, по окончанию таймслота, пропускать «снег» и сбрасывать деградацию сигнала. Настройки — в окне прямо в игре.

[English README](README.en.md)

```text
F10 → окно настроек → включить нужные режимы → смотреть ТВ
```

| Возможность | Что делает |
| --- | --- |
| Переключение по таймеру | Каждые N минут переключает канал вниз на экране трансляции |
| Автопереключение | Переключает канал вниз после полного цикла из 5 передач (~5 мин), а не после каждой |
| Пропуск снега и помех | Снег (CMBR), слабый сигнал (MEGA), locked ACCESS (72), Femtofax (21), титры (25) |
| Отключить деградацию сигнала | Раз в 120 с сбрасывает `signalLossInProgress` в `false` |
| Окно настроек (F10) | Полоска каналов (пред. > текущий > след.), язык UI, чекбоксы, слайдер, отсчёт |
| Горячие клавиши | F9 — вкл/выкл таймер, `[` / `]` — интервал ±1 мин |
| Сохранение | Настройки пишутся в `UserData/MelonPreferences.cfg` |
| Безопасность | Переключение каналов не работает в меню, гиде программ и модальных окнах |

> Только PC (Steam). Нужны Blippo+ и MelonLoader.

---

## Quick Start

### 1. Установите игру

Купите и установите **Blippo+** в Steam. Запустите один раз и закройте.

### 2. Установите MelonLoader

1. Скачайте [MelonLoader.Installer.exe](https://github.com/LavaGang/MelonLoader/releases/latest).
2. Запустите установщик.
3. Выберите `Blippo+.exe` из папки игры:

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\Blippo+.exe
```

4. Нажмите **Install** и дождитесь окончания.

### 3. Установите мод

**Вариант A — из релиза (проще)**

1. Откройте [Releases](https://github.com/Marfa/blippo_channel_switcher/releases).
2. Скачайте `BlippoChannelHopper.dll` из релиза **v1.2.0**.
3. Положите файл в папку `Mods` внутри каталога игры:

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\Mods\BlippoChannelHopper.dll
```

Если папки `Mods` нет — создайте её.

**Вариант B — собрать из исходников**

```powershell
git clone https://github.com/Marfa/blippo_channel_switcher.git
cd blippo_channel_switcher
dotnet build BlippoChannelHopper.csproj -c Release
```

Скопируйте `bin\BlippoChannelHopper.dll` в `Mods` игры.

### 4. Запустите игру

1. Запустите Blippo+ через Steam.
2. Дойдите до экрана **просмотра ТВ** (broadcast).
3. Нажмите **F10** — откроется окно мода.
4. Включите нужные режимы (например **Переключение по таймеру**) и при необходимости задайте интервал.

---

## Управление

| Клавиша | Действие |
| --- | --- |
| **F10** | Окно настроек |
| **F9** | Вкл / выкл **переключение по таймеру** |
| **`[`** | Интервал −1 мин |
| **`]`** | Интервал +1 мин |

В окне настроек:

- **полоска каналов**: `пред. > текущий > след.` и строка **«Переключение через: N мин»** (отсчёт активного режима — таймер *или* автопереключение)
- **язык UI**: Авто (по системе) / Русский / English
- **Переключение по таймеру** — каждые N минут (нельзя включить вместе с автопереключением)
- **Автопереключение** — после полного цикла из 5 передач (~5 мин)
- **Пропуск снега и помех** — CMBR, MEGA, locked ACCESS на 72, Femtofax (21), титры (25); «след.» канал учитывает цепочку пропусков
- **Отключить деградацию сигнала** — сброс флага каждые 120 с
- слайдер 1–60 мин, кнопки **−1 мин** / **+1 мин**, статусы

**Переключение по таймеру** и **Автопереключение** взаимоисключающие — включение одного выключает другой. **Пропуск снега и помех** и **Отключить деградацию сигнала** работают независимо и могут сочетаться с любым режимом переключения.

Таймер **не сбрасывается** при ручном переключении каналов пультом.

---

## Где лежат настройки

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\UserData\MelonPreferences.cfg
```

Пример:

```toml
[BlippoChannelHopper]
Enabled = false
HopOnBroadcastEnd = false
SkipSnow = false
DisableSignalLoss = false
IntervalMinutes = 1.0
UiLanguage = 0
```

`UiLanguage`: `0` — авто, `1` — русский, `2` — английский.

Редактировать файл вручную не обязательно — всё настраивается через **F10**.

---

## Сборка

Требования: [.NET SDK](https://dotnet.microsoft.com/download) (6.0+).

По умолчанию проект ссылается на игру в стандартной папке Steam. Другой путь:

```powershell
dotnet build BlippoChannelHopper.csproj -c Release /p:BlippoGameDir="D:\Games\Blippo+"
```

Для сборки нужны DLL из установленной игры (`Blippo+_Data\Managed`) и MelonLoader в папке игры (`MelonLoader\net35\MelonLoader.dll`). Либо распакуйте [MelonLoader.x64.zip](https://github.com/LavaGang/MelonLoader/releases/latest) и укажите:

```powershell
dotnet build -c Release /p:MelonLoaderDir="путь\к\MelonLoader\MelonLoader"
```

После успешной сборки DLL копируется в `Mods` игры, если папка существует.

---

## Ограничения

- Переключение каналов работает только на экране **трансляции** (не в EPG и не в меню).
- Сброс деградации сигнала только выставляет `signalLossInProgress = false` раз в 120 с; уже сдвинутые параметры тюнера сами не откатываются.
- Неофициальный мод; Panic / Steam Workshop его не поддерживают.
- MelonLoader — сторонний загрузчик; используйте на свой риск.

---

## Автор

Код подготовлен с помощью [Cursor](https://cursor.com).

Поддержка проекта:
- [DonationAlerts](https://www.donationalerts.com/r/themarfa)
- [Криптодонат NOWPayments](https://nowpayments.io/donation/themarfa)

## Лицензия

**Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0)**

См. [LICENSE](LICENSE) · https://creativecommons.org/licenses/by-nc-sa/4.0/
