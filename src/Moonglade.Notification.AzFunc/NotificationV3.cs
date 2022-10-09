using System.Text.Json;
using Azure.Storage.Queues.Models;
using Edi.TemplateEmail;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moonglade.Notification.AzFunc.Core;
using Moonglade.Notification.AzFunc.Payloads;

namespace Moonglade.Notification.AzFunc;

public class NotificationV3
{
    [FunctionName("NotificationV3")]
    public async Task Run(
        [QueueTrigger("moongladeemailqueue", Connection = "moongladestorage")] QueueMessage queueMessage,
        ILogger log,
        Microsoft.Azure.WebJobs.ExecutionContext executionContext)
    {
        log.LogInformation($"C# Queue trigger function processed: {queueMessage.MessageId}");

        try
        {
            var en = JsonSerializer.Deserialize<EmailNotificationV3>(queueMessage.MessageText);

            if (en != null)
            {
                log.LogInformation($"Found message: {queueMessage.MessageId}");

                if (string.IsNullOrWhiteSpace(en.DistributionList))
                {
                    log.LogError($"Message Id '{queueMessage.MessageId}' has no DistributionList, operation aborted.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(en.MessageType))
                {
                    log.LogError($"Message Id '{queueMessage.MessageId}' has no MessageType, operation aborted.");
                    return;
                }

                var configSource = Path.Join(executionContext.FunctionAppDirectory, "mailConfiguration.xml");
                if (!File.Exists(configSource))
                    throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

                var emailHelper = new EmailHelper(
                    configSource,
                    Environment.GetEnvironmentVariable("SmtpServer"),
                    Environment.GetEnvironmentVariable("SmtpUserName"),
                    Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
                    int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "587"));

                if (bool.Parse(Environment.GetEnvironmentVariable("EnableTls") ?? "true")) emailHelper.WithTls();

                emailHelper.EmailSent += async (sender, eventArgs) =>
                {
                    if (sender is MimeMessage msg)
                    {
                        log.LogInformation($"Email '{msg.Subject}' is sent. Success: {eventArgs.IsSuccess}");
                    }
                };

                emailHelper.EmailFailed += (sender, args) =>
                {
                    if (sender is MimeMessage msg)
                    {
                        log.LogError($"Email '{msg.Subject}' failed: {args.ServerResponse}");
                        throw new(args.ServerResponse);
                    }
                };

                var dName = Environment.GetEnvironmentVariable("SenderDisplayName");
                var notification = new EmailHandler(emailHelper, dName);
                log.LogInformation($"Sending {en.MessageType} message");

                switch (en.MessageType)
                {
                    case "TestMail":
                        await notification.SendTestNotificationAsync(en.DistributionList.Split(';'));
                        break;

                    case "NewCommentNotification":
                        var ncPayload = JsonSerializer.Deserialize<NewCommentPayload>(en.MessageBody);
                        await notification.SendNewCommentNotificationAsync(en.DistributionList.Split(';'), ncPayload);
                        break;

                    case "AdminReplyNotification":
                        var replyPayload = JsonSerializer.Deserialize<CommentReplyPayload>(en.MessageBody);
                        await notification.SendCommentReplyNotificationAsync(en.DistributionList, replyPayload);
                        break;

                    case "BeingPinged":
                        var pingPayload = JsonSerializer.Deserialize<PingPayload>(en.MessageBody);
                        await notification.SendPingNotificationAsync(en.DistributionList.Split(';'), pingPayload);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            throw;
        }
    }
}

public class EmailNotificationV3
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
    public int SendingStatus { get; set; }
    public DateTime? SentTimeUtc { get; set; }
    public string TargetResponse { get; set; }
}