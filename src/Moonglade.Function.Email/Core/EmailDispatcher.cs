using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;

namespace Moonglade.Function.Email.Core;

public class EmailDispatcher(EmailSettings smtpSettings, ILogger<EmailDispatcher> logger) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message)
    {
        var provider = EnvHelper.Get<string>("MOONGLADE_EMAIL_PROVIDER");
        var sender = string.IsNullOrWhiteSpace(provider) ? "smtp" : provider.ToLowerInvariant();

        switch (sender)
        {
            case "smtp":
                var response = await message.SendAsync(smtpSettings);
                logger.LogInformation("SMTP response: {Response}", response);
                break;

            case "azurecommunication":
                var result = await message.SendAzureCommunicationAsync();
                logger.LogInformation("AzureCommunication operation ID: {OperationId}", result.Id);
                break;

            default:
                throw new InvalidOperationException($"Email provider '{sender}' is not supported.");
        }
    }
}
