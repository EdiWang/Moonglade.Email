using Azure;
using Azure.Communication.Email;
using Edi.TemplateEmail;
using Microsoft.Extensions.Options;
using Moonglade.Function.Email.Core;

namespace Moonglade.Function.Email;

public class AzureCommunicationSender(IOptions<EmailServiceOptions> options)
{
    private readonly Lazy<EmailClient> _client =
        new(() => new EmailClient(options.Value.AcsConnectionString));

    public async Task<EmailSendOperation> SendAsync(CommonMailMessage message)
    {
        var emailMessage = new EmailMessage(
            senderAddress: options.Value.AcsSenderAddress,
            content: new EmailContent(message.Subject),
            recipients: new EmailRecipients(message.Receipts.Select(p => new EmailAddress(p))));

        if (message.BodyIsHtml)
        {
            emailMessage.Content.Html = message.Body;
        }
        else
        {
            emailMessage.Content.PlainText = message.Body;
        }

        return await _client.Value.SendAsync(WaitUntil.Completed, emailMessage);
    }
}
