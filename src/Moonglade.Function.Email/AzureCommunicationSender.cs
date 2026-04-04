using Azure;
using Azure.Communication.Email;
using Edi.TemplateEmail;

namespace Moonglade.Function.Email;

public class AzureCommunicationSender
{
    public static async Task<EmailSendOperation> SendAsync(
        CommonMailMessage message,
        string connectionString,
        string senderAddress)
    {
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
