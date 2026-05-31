using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moonglade.Function.Email.Core;

public class EmailDispatcher(
    EmailSettings smtpSettings,
    IOptions<EmailServiceOptions> options,
    AzureCommunicationSender acsSender,
    ILogger<EmailDispatcher> logger) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message)
    {
        var sender = options.Value.NormalizedProvider;

        switch (sender)
        {
            case EmailServiceOptions.SmtpProvider:
                var response = await message.SendAsync(smtpSettings);
                logger.LogInformation("SMTP response: {Response}", response);
                break;

            case EmailServiceOptions.AzureCommunicationProvider:
                var result = await acsSender.SendAsync(message);
                logger.LogInformation("AzureCommunication operation ID: {OperationId}", result.Id);
                break;

            default:
                throw new InvalidOperationException($"Email provider '{sender}' is not supported.");
        }
    }
}
