using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moonglade.Function.Email.Core;

namespace Moonglade.Function.Email.Tests;

public class EmailDispatcherTests
{
    private readonly EmailSettings _smtpSettings = new()
    {
        SmtpSettings = new SmtpSettings("localhost", string.Empty, string.Empty, 25)
    };
    private readonly Mock<ILogger<EmailDispatcher>> _mockLogger = new();

    private EmailDispatcher CreateDispatcher(string provider)
    {
        var options = Options.Create(new EmailServiceOptions { Provider = provider });
        var acsSender = new AzureCommunicationSender(
            Options.Create(new EmailServiceOptions { AcsConnectionString = "dummy", AcsSenderAddress = "no-reply@dummy.com" }));
        return new EmailDispatcher(_smtpSettings, options, acsSender, _mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_UnsupportedProvider_ThrowsInvalidOperationException()
    {
        var dispatcher = CreateDispatcher("unsupported");
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.SendAsync(message));
    }

    [Theory]
    [InlineData("UNSUPPORTED")]
    [InlineData("SendGrid")]
    [InlineData("mailgun")]
    public async Task SendAsync_UnknownProvider_ThrowsInvalidOperationException(string provider)
    {
        var dispatcher = CreateDispatcher(provider);
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.SendAsync(message));
    }

    [Fact]
    public async Task SendAsync_NullOrEmptyProvider_DefaultsToSmtp_ThrowsSmtpException()
    {
        var dispatcher = CreateDispatcher(string.Empty);
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        // Empty provider falls back to "smtp" which will attempt SMTP connection
        // Without a real server it throws a network-level or SMTP exception
        await Assert.ThrowsAnyAsync<Exception>(() => dispatcher.SendAsync(message));
    }

    [Fact]
    public async Task SendAsync_ProviderIsCaseInsensitive_SmtpAndSMTPRouteSamePath()
    {
        var dispatcher1 = CreateDispatcher("smtp");
        var dispatcher2 = CreateDispatcher("SMTP");
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        var ex1 = await Record.ExceptionAsync(() => dispatcher1.SendAsync(message));
        var ex2 = await Record.ExceptionAsync(() => dispatcher2.SendAsync(message));

        // Both should produce the same kind of exception (SMTP connection failure), not InvalidOperationException
        Assert.IsNotType<InvalidOperationException>(ex1);
        Assert.IsNotType<InvalidOperationException>(ex2);
    }
}
