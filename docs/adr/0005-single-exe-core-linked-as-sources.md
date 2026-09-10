# ADR-0005: Лаунчер — один exe: Core компилируется в него исходниками, JSON — `DataContractJsonSerializer`

- **Статус**: принято
- **Дата**: 2026-09-10

## Контекст

Самообновление ([ADR-0003](0003-self-update-by-renaming-running-exe.md)) заменяет ровно один
файл — exe лаунчера. Любая dll рядом с ним либо не обновится, либо потребует своей схемы замены.
`Launcher.Core` на `System.Text.Json` в `net48`-приложении тянет за собой восемь сборок
(`System.Memory`, `System.Buffers`, `Microsoft.Bcl.AsyncInterfaces`, …) и `exe.config` с
binding redirect'ами.

## Решение

- `Launcher.App.csproj` включает `..\Launcher.Core\**\*.cs` как связанные исходники (`<Compile Link>`),
  а не ссылку на проект. Core собирается внутрь `FloxLauncher.exe`.
- JSON в Core — `System.Runtime.Serialization.Json.DataContractJsonSerializer`, встроенный и в
  .NET Framework, и в `netstandard2.0`. Модели размечены `[DataContract]`/`[DataMember(Name = "…")]`.
- Сериализатор создаёт объекты без конструктора, поэтому инициализаторы свойств не срабатывают.
  Строки и списки моделей лежат в nullable-полях, а геттеры отдают `""` / пустой список вместо
  `null` — отсутствующее в JSON поле не превращается в `NullReferenceException`.

Итог: `FloxLauncher.exe` около 85 КБ и больше ничего. `.config` с `supportedRuntime` не
генерируется (`GenerateSupportedRuntime=false`): версия CLR и так записана в заголовке exe, а
отличие только в том, что на машине без .NET Framework 4.8 Windows не предложит его поставить, и
лаунчер упадёт при старте. Windows 10/11 с обновлениями несут 4.8 из коробки.

## Рассмотренные альтернативы

**Costura.Fody (dll внутрь exe ресурсами).** Сохраняет `System.Text.Json`. Отвергнуто: exe ~1,5 МБ,
подгрузка через `AssemblyResolve` в обход binding redirect'ов, пакет в режиме поддержки.

**ILMerge / ILRepack.** Шаг пост-сборки, ломкий с WPF (BAML ссылается на имя сборки).

**Папка dll рядом с exe.** Самообновление должно будет менять набор файлов — по сути второй
`GameUpdater` для лаунчера.

**Ручной парсер JSON.** Свой код там, где есть встроенный.

## Последствия

- В `manifest.json` слэши экранированы (`"media\/…"`) — это валидный JSON, лаунчер и `ConvertFrom-Json`
  читают его без проблем.
- Внутренние типы Core (`FileOps`, `CountingStream`) видны коду окна как `internal` его собственной
  сборки. Пользоваться ими из `Launcher.App` не принято: граница — публичный API Core.
- Тесты по-прежнему гоняют Core как отдельную `netstandard2.0`-сборку.
