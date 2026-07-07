# **Автопереключение каналов в Blippo+ — по таймеру, без пульта**

MelonLoader-мод для [Blippo+](https://store.steampowered.com/app/3323850/Blippo/): сам листает каналы вниз, пока вы смотрите эфир. Интервал — в минутах, настройки — в окне прямо в игре.

[English README](README.en.md)

```text
F10 → окно настроек → включить → задать интервал → смотреть ТВ
```

| Возможность | Что делает |
| --- | --- |
| Автопереключение | Каждые N минут переключает канал вниз на экране трансляции |
| Окно настроек (F10) | Вкл/выкл, слайдер интервала 1–60 мин, таймер до следующего переключения |
| Горячие клавиши | F9 — быстро вкл/выкл, `[` / `]` — интервал ±1 мин |
| Сохранение | Настройки пишутся в `UserData/MelonPreferences.cfg` |
| Безопасность | Не работает в меню, гиде программ и модальных окнах |

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
2. Скачайте `BlippoChannelHopper.dll` из релиза **v1.0.0**.
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
4. Включите **Auto channel hop** и задайте интервал в минутах.

---

## Управление

| Клавиша | Действие |
| --- | --- |
| **F10** | Окно настроек |
| **F9** | Вкл / выкл автопереключение |
| **`[`** | Интервал −1 мин |
| **`]`** | Интервал +1 мин |

В окне настроек: чекбокс, слайдер 1–60 мин, кнопки **-1 min** / **+1 min**, обратный отсчёт до следующего канала.

---

## Где лежат настройки

```text
C:\Program Files (x86)\Steam\steamapps\common\Blippo+\UserData\MelonPreferences.cfg
```

Пример:

```toml
[BlippoChannelHopper]
Enabled = false
IntervalMinutes = 1.0
```

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

---

## Ограничения

- Работает только на экране **трансляции** (не в EPG и не в меню).
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
