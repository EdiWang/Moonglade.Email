using Azure;
using Azure.Communication.Email;
using Edi.TemplateEmail;

namespace Moonglade.Function.Email;

public class AzureCommunicationSender
{
    public static async Task<EmailSendOperation> SendAsync(CommonMailMessage message)
    {
        var connectionString = EnvHelper.Get<string>("MOONGLADE_EMAIL_ACS_CONN");
        var senderAddress = EnvHelper.Get<string>("MOONGLADE_EMAIL_ACS_ADDR");

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
    public static Task<EmailSendOperation> SendAzureCommunicationAsync(this CommonMailMessage message)
    {
        return AzureCommunicationSender.SendAsync(message);
    }
}