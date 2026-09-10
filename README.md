# Flox Launcher

Лёгкий лаунчер для закрытых demo-сборок игр Flox Studios под Windows. Показывает скриншот и
список изменений, предлагает обновиться и запускает игру. Первая игра — Kintsugi Master
(репозиторий PotteryBraker).

Сборки лежат на обычном хостинге как статика. Серверного кода нет: лаунчер скачивает
`manifest.json`, сравнивает его с установленными файлами и докачивает только изменившиеся.

## Состав

| Путь | Что это |
|---|---|
| `src/Launcher.Core/` | Вся логика без UI: манифест, сканирование установки, план обновления, загрузка с проверкой SHA-256, политика запуска, самообновление, сборка релиза. `netstandard2.0`. |
| `src/Launcher.Publish/` | Консольная утилита: превращает папку сборки игры в раскладку сайта (`objects/`, `media/`, `launcher/`, `manifest.json`). |
| `tools/publish.ps1` | Собирает раскладку через `Launcher.Publish` и заливает её на хостинг по SFTP через WinSCP. |
| `tests/Launcher.Tests/` | xUnit-тесты Core. |
| `src/Launcher.App/` | WPF-окно на .NET Framework 4.8. Core компилируется внутрь, на выходе один `FloxLauncher.exe` (~85 КБ). |
| `installer/` | Скрипт Inno Setup — **ещё не написан**. |

## Документация

- [docs/how-it-works.md](docs/how-it-works.md) — как устроены сервер, манифест, обновление, запуск и публикация.
- [docs/adr/](docs/adr/README.md) — решения и отвергнутые альтернативы.

## Требования

- .NET 8 SDK — сборка всех проектов, включая `net48` (референсные сборки приходят NuGet-пакетом).
- WinSCP — заливка на хостинг.
- Inno Setup 6 — сборка установщика.

## Команды

```powershell
dotnet build
dotnet test

./tools/publish.ps1 -BuildDir "..\PotteryBraker\My project\Build\StandaloneWindows64" `
    -Version 0.0.11 -Exe PotteryBraker.exe -Screenshot shot.png -Changelog changelog.md
```

Параметры SFTP-доступа — в переменных окружения, см. [docs/how-it-works.md](docs/how-it-works.md#публикация).

## Правила

- В коде нет комментариев; «почему» живёт в `docs/`.
- Стиль C# повторяет соглашения PotteryBraker: явный `private`, `_camelCase` для полей,
  `UPPER_SNAKE_CASE` для констант, Allman, без expression-bodied методов, `var` только когда тип
  виден справа.
- Документация и сообщения коммитов — на русском, код — на английском.
