using StangaNetLib.Core.Common;

namespace StangaNetLib.Notifications.Tests;

public sealed class SimpleTemplateEngineTests
{
    private readonly SimpleTemplateEngine _engine = new();

    [Fact]
    public void Resolve_RegisteredTemplate_ReturnsSuccess()
    {
        _engine.Register(new NotificationTemplate
        {
            TemplateId = "greet",
            Channel = NotificationChannel.Email,
            BodyTemplate = "Hello {{Name}}",
        });

        var result = _engine.Resolve("greet", NotificationChannel.Email);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TemplateId.Should().Be("greet");
    }

    [Fact]
    public void Resolve_UnregisteredTemplate_ReturnsNotFound()
    {
        var result = _engine.Resolve("missing", NotificationChannel.Email);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Resolve_SameIdDifferentChannel_ReturnsNotFound()
    {
        _engine.Register(new NotificationTemplate
        {
            TemplateId = "greet",
            Channel = NotificationChannel.Email,
            BodyTemplate = "Hello",
        });

        var result = _engine.Resolve("greet", NotificationChannel.Push);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Render_AllPlaceholdersResolved_ReturnsRenderedContent()
    {
        var template = new NotificationTemplate
        {
            TemplateId = "t",
            Channel = NotificationChannel.Email,
            SubjectTemplate = "Hi {{Name}}",
            BodyTemplate = "Dear {{Name}}, your code is {{Code}}.",
        };
        var data = new Dictionary<string, string> { ["Name"] = "Bob", ["Code"] = "1234" };

        var result = _engine.Render(template, data);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Subject.Should().Be("Hi Bob");
        result.Value.Body.Should().Be("Dear Bob, your code is 1234.");
    }

    [Fact]
    public void Render_MissingPlaceholderData_LeavesPlaceholderAsIs()
    {
        var template = new NotificationTemplate
        {
            TemplateId = "t",
            Channel = NotificationChannel.Email,
            BodyTemplate = "Hello {{Name}}, code: {{Code}}.",
        };
        var data = new Dictionary<string, string> { ["Name"] = "Alice" };

        var result = _engine.Render(template, data);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Body.Should().Be("Hello Alice, code: {{Code}}.");
    }

    [Fact]
    public void Render_NoSubjectTemplate_ReturnsNullSubject()
    {
        var template = new NotificationTemplate
        {
            TemplateId = "t",
            Channel = NotificationChannel.InApp,
            BodyTemplate = "You have a message.",
        };

        var result = _engine.Render(template, new Dictionary<string, string>());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Subject.Should().BeNull();
        result.Value.Body.Should().Be("You have a message.");
    }

    [Fact]
    public void Register_OverwritesExistingTemplate()
    {
        _engine.Register(new NotificationTemplate
        {
            TemplateId = "t",
            Channel = NotificationChannel.Email,
            BodyTemplate = "Old body",
        });
        _engine.Register(new NotificationTemplate
        {
            TemplateId = "t",
            Channel = NotificationChannel.Email,
            BodyTemplate = "New body",
        });

        var resolved = _engine.Resolve("t", NotificationChannel.Email);
        resolved.Value!.BodyTemplate.Should().Be("New body");
    }

    [Fact]
    public void Render_EmptyData_RendersBodyWithNoSubstitution()
    {
        var template = new NotificationTemplate
        {
            TemplateId = "t",
            Channel = NotificationChannel.Email,
            BodyTemplate = "Static body.",
        };

        var result = _engine.Render(template, new Dictionary<string, string>());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Body.Should().Be("Static body.");
    }
}
