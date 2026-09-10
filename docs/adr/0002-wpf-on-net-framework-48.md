# ADR-0002: Окно — WPF на .NET Framework 4.8, логика — `netstandard2.0`

- **Статус**: принято
- **Дата**: 2026-09-10

## Контекст

Лаунчер должен быть лёгким и ставиться у тестеров без лишних шагов. Платформа — только Windows.
Команда пишет на C#.

## Решение

`Launcher.App` — WPF на `net48`. .NET Framework 4.8 встроен в Windows 10 (1903+) и 11, поэтому exe
весит сотни килобайт и не требует рантайма. Вся логика — в `Launcher.Core` на `netstandard2.0`:
её используют и `net48`-окно, и `net8.0`-утилита публикации, и `net8.0`-тесты. Сборка — .NET 8 SDK,
референсные сборки `net48` приходят пакетом `Microsoft.NETFramework.ReferenceAssemblies`.

## Рассмотренные альтернативы

**WPF на .NET 8, self-contained.** Современный рантайм. Отвергнуто: 60+ МБ, trimming WPF не
поддерживается.

**WPF на .NET 8, framework-dependent.** Маленький exe. Отвергнуто: у тестера должен стоять .NET 8
Desktop Runtime — лишний шаг установщика и частая причина «не запускается».

**Avalonia.** Кроссплатформенность, которая сейчас не нужна, ценой 30–60 МБ.

**Tauri / Electron.** Красивый UI на HTML. Tauri — новый стек (Rust), Electron — 100+ МБ.

## Последствия

- В `Launcher.Core` недоступны API новее `netstandard2.0` (`Path.GetRelativePath`,
  `File.Move(…, overwrite)`, `Convert.ToHexString`) — вместо них внутренние хелперы `FileOps`.
- Nullable-аннотации BCL в `netstandard2.0` неполные; проверки на пустую строку пишутся явными
  сравнениями.
