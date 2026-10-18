using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Notifications.Abstractions;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Implementations;

/// <summary>
/// <see cref="INotificationTemplateEngine"/> implementation that stores templates in a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
/// and substitutes <c>{{Key}}</c> placeholders via a compiled <see cref="System.Text.RegularExpressions.Regex"/>.
/// Unknown placeholders are left unchanged. Suitable for in-process use; templates are not persisted across restarts.
/// </summary>
internal sealed class SimpleTemplateEngine : INotificationTemplateEngine
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
    private readonly ConcurrentDictionary<string, NotificationTemplate> _templates = new(StringComparer.Ordinal);

    public void Register(NotificationTemplate template)
    {
        Guard.Against.Null(template, nameof(template));
        Guard.Against.NullOrWhiteSpace(template.TemplateId, nameof(template.TemplateId));
        Guard.Against.NullOrWhiteSpace(template.BodyTemplate, nameof(template.BodyTemplate));
        _templates[BuildKey(template.TemplateId, template.Channel)] = template;
    }

    public Result<NotificationTemplate> Resolve(string templateId, NotificationChannel channel)
    {
        Guard.Against.NullOrWhiteSpace(templateId, nameof(templateId));
        var key = BuildKey(templateId, channel);
        return _templates.TryGetValue(key, out var template)
            ? Result<NotificationTemplate>.Success(template)
            : Result<NotificationTemplate>.Failure(Error.NotFound("NotificationTemplate", key));
    }

    public Result<RenderedNotification> Render(NotificationTemplate template, IReadOnlyDictionary<string, string> data)
    {
        Guard.Against.Null(template, nameof(template));
        Guard.Against.Null(data, nameof(data));

        if (string.IsNullOrWhiteSpace(template.BodyTemplate))
            return Result<RenderedNotification>.Failure(Error.Validation("BodyTemplate", "Template body must not be empty."));

        var body = RenderString(template.BodyTemplate, data);
        var subject = template.SubjectTemplate is not null ? RenderString(template.SubjectTemplate, data) : null;
        return Result<RenderedNotification>.Success(new RenderedNotification(subject, body));
    }

    private static string RenderString(string template, IReadOnlyDictionary<string, string> data)
        => PlaceholderRegex.Replace(template, m =>
            data.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    private static string BuildKey(string templateId, NotificationChannel channel)
        => $"{templateId}:{channel}";
}
