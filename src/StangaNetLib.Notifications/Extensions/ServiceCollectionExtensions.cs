using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StangaNetLib.Notifications.Abstractions;
using StangaNetLib.Notifications.Implementations;
using StangaNetLib.Notifications.Settings;
using StangaNetLib.Resilience.Execution;

namespace StangaNetLib.Notifications.Extensions;

/// <summary>
/// Extension methods for registering StangaNetLib.Notifications services.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// {
///   "NotificationsSettings": {
///     "MaxRetryCount": 3,
///     "RetryDelay": "00:00:05"
///   }
/// }
/// </code>
/// Program.cs:
/// <code>
/// builder.Services.AddStangaNetLibNotifications(builder.Configuration);
///
/// // Register one or more channel providers:
/// builder.Services.AddTransient&lt;INotificationChannelProvider, SmtpEmailProvider&gt;();
/// builder.Services.AddTransient&lt;INotificationChannelProvider, FirebasePushProvider&gt;();
///
/// var app = builder.Build();
///
/// // Register templates after the container is built (optional):
/// app.Services.GetRequiredService&lt;INotificationTemplateEngine&gt;().Register(new NotificationTemplate
/// {
///     TemplateId = "welcome",
///     Channel = NotificationChannel.Email,
///     SubjectTemplate = "Welcome, {{UserName}}!",
///     BodyTemplate = "Hi {{UserName}}, thanks for joining.",
/// });
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers StangaNetLib.Notifications with the built-in in-memory repository.
    /// Uses configuration from the <c>NotificationsSettings</c> section of <paramref name="configuration"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<NotificationsSettings>(configuration.GetSection(NotificationsSettings.SectionName));
        return RegisterCore(services);
    }

    /// <summary>
    /// Registers StangaNetLib.Notifications with the built-in in-memory repository using default settings.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibNotifications(this IServiceCollection services)
    {
        services.Configure<NotificationsSettings>(_ => { });
        return RegisterCore(services);
    }

    private static IServiceCollection RegisterCore(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<NotificationsSettings>, NotificationsSettingsValidator>();
        services.TryAddSingleton<INotificationTemplateEngine, SimpleTemplateEngine>();
        services.TryAddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        services.TryAddSingleton<INotificationSender>(sp => new NotificationDispatcher(
            sp.GetRequiredService<IEnumerable<INotificationChannelProvider>>(),
            sp.GetRequiredService<INotificationTemplateEngine>(),
            sp.GetRequiredService<INotificationRepository>(),
            sp.GetRequiredService<IOptions<NotificationsSettings>>(),
            sp.GetService<IResilientExecutor>()));
        return services;
    }
}
