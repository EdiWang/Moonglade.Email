using Azure;
using Azure.Communication.Email;
using Edi.TemplateEmail;

namespace Moonglade.Function.Email;

public class AzureCommunicationSender
{
    public async Task<EmailSendOperation> SendAsync(CommonMailMessage message)
    {
        var connectionString = EnvHelper.Get<string>("AzureCommunicationConnection");
        var senderAddress = EnvHelper.Get<string>("AzureCommunicationSenderAddress");

        var emailClient = new EmailClient(connectionString);

        var emailMessage = new EmailMessage(
            senderAddress: senderAddress,
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

        var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
        return emailSendOperation;
    }
}

public static class CommonMailMessageExtensions
{
    public static async Task SendAzureCommunicationAsync(this CommonMailMessage message)
    {
        var sender = new AzureCommunicationSender();
        await sender.SendAsync(message);
    }
}