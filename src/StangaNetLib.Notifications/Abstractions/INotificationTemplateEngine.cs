using StangaNetLib.Core.Common;
using StangaNetLib.Notifications.Models;

namespace StangaNetLib.Notifications.Abstractions;

/// <summary>
/// Manages notification templates: registration, resolution by ID and channel, and rendering with data substitution.
/// </summary>
public interface INotificationTemplateEngine
{
    /// <summary>
    /// Registers a template. If a template with the same <see cref="NotificationTemplate.TemplateId"/>
    /// and <see cref="NotificationTemplate.Channel"/> already exists, it is replaced.
    /// </summary>
    /// <param name="template">The template to register. Must have a non-empty TemplateId and BodyTemplate.</param>
    void Register(NotificationTemplate template);

    /// <summary>
    /// Resolves a previously registered template by its ID and target channel.
    /// Templates are scoped per channel: the same <paramref name="templateId"/> may exist for multiple channels.
    /// </summary>
    /// <param name="templateId">The unique template identifier.</param>
    /// <param name="channel">The delivery channel to look up.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the template,
    /// or <c>NotificationTemplate.NotFound</c> if no matching template is registered.
    /// </returns>
    Result<NotificationTemplate> Resolve(string templateId, NotificationChannel channel);

    /// <summary>
    /// Renders a template by substituting <c>{{Key}}</c> placeholders with values from <paramref name="data"/>.
    /// Unknown placeholders are left as-is. Rendering is always synchronous.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="data">Key/value pairs used to resolve placeholders.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the <see cref="RenderedNotification"/>,
    /// or a failure if the template body is empty.
    /// </returns>
    Result<RenderedNotification> Render(NotificationTemplate template, IReadOnlyDictionary<string, string> data);
}
