using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moonglade.Function.Email.Core;

public class EmailDispatcher(
    EmailSettings smtpSettings,
    IOptions<EmailServiceOptions> options,
    ILogger<EmailDispatcher> logger) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message)
    {
        var opts = options.Value;
        var sender = string.IsNullOrWhiteSpace(opts.Provider) ? "smtp" : opts.Provider.ToLowerInvariant();

        switch (sender)
        {
            case "smtp":
                var response = await message.SendAsync(smtpSettings);
                logger.LogInformation("SMTP response: {Response}", response);
                break;

            case "azurecommunication":
                var result = await AzureCommunicationSender.SendAsync(
                    message,
                    opts.AcsConnectionString,
                    opts.AcsSenderAddress);
                logger.LogInformation("AzureCommunication operation ID: {OperationId}", result.Id);
                break;

            default:
                throw new InvalidOperationException($"Email provider '{sender}' is not supported.");
        }
    }
}
