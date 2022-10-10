using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moonglade.Notification.AzFunc.Core;
using Moonglade.Notification.AzFunc.Payloads;
using System.Text.Json;

namespace Moonglade.Notification.AzFunc;

public class NotificationStorageQueue
{
    [FunctionName("NotificationStorageQueue")]
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

                var emailHelper = Helper.GetEmailHelper(executionContext.FunctionAppDirectory);

                emailHelper.EmailSent += (sender, eventArgs) =>
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
}