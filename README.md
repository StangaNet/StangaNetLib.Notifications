# StangaNetLib.Notifications

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Notifications/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Notifications/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.1-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)

Multi-channel notification dispatch (email, in-app, push) with template rendering, retry, and delivery tracking. Uses `Result` / `Guard` from [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core); optional send resilience via [`StangaNetLib.Resilience`](https://github.com/StangaNet/StangaNetLib.Resilience).

## Installation

The package is hosted on **GitHub Packages** (pulls Core/Resilience from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Notifications" Version="1.0.0" />
```

Requires `StangaNetLib.Resilience` **1.0.0** (not 1.0.1). See [CHANGELOG.md](./CHANGELOG.md).

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.Notifications.Abstractions` | `INotificationSender`, `INotificationChannelProvider`, `INotificationTemplateEngine`, `INotificationRepository` | Contracts |
| `StangaNetLib.Notifications.Models` | `NotificationRequest`, `NotificationRecord`, `NotificationTemplate`, … | Domain models |
| `StangaNetLib.Notifications.Settings` | `NotificationsSettings` | Options |
| `StangaNetLib.Notifications.Extensions` | `AddStangaNetLibNotifications` | DI |

## Quick Start

```csharp
builder.Services.AddStangaNetLibNotifications(builder.Configuration);
builder.Services.AddTransient<INotificationChannelProvider, SmtpEmailProvider>();
// Optional: builder.Services.AddStangaNetLibResilience(...) for pipeline "Notifications"
```

## License

Apache-2.0 — see [LICENSE](./LICENSE) and [NOTICE](./NOTICE).
