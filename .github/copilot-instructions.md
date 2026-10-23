# StangaNetLib.Notifications — Copilot instructions

- Multi-channel notifications: dispatch, templates, repository, channel providers.
- Prefer `Result` / `Result<T>` and `Guard` from `StangaNetLib.Core`.
- Optional resilience: `IResilientExecutor` from `StangaNetLib.Resilience.Execution` (pipeline `"Notifications"`).
- Keep public APIs documented (`///`) and options-driven via `NotificationsSettings`.
- DI: `AddStangaNetLibNotifications`; register `INotificationChannelProvider` implementations separately.
- Pin `StangaNetLib.Resilience` 1.0.0 (not 1.0.1); never reintroduce ME 10.x.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions; cover `net8.0`, `net9.0`, and `net10.0` when changing shared behavior.
