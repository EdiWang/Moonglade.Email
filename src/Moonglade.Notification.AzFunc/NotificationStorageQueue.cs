using Azure.Storage.Queues.Models;
using MailKit.Net.Smtp;
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

                var sendingMode = 1;
                var envSendingMode = Environment.GetEnvironmentVariable("DistributionListSendingMode");
                if (!string.IsNullOrWhiteSpace(envSendingMode))
                {
                    bool isParsed = int.TryParse(envSendingMode, out sendingMode);
                    if (!isParsed)
                    {
                        log.LogWarning("Failed to parse 'DistributionListSendingMode', falling back to '1', please check settings.");
                    }
                }

                switch (en.MessageType)
                {
                    case "TestMail":
                        await notification.SendTestNotificationAsync(en.DistributionList.Split(';'));

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendTestNotificationAsync(x);
                        }, log);

                        break;

                    case "NewCommentNotification":
                        var ncPayload = JsonSerializer.Deserialize<NewCommentPayload>(en.MessageBody);

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendNewCommentNotificationAsync(x, ncPayload);
                        }, log);

                        break;

                    case "AdminReplyNotification":
                        var replyPayload = JsonSerializer.Deserialize<CommentReplyPayload>(en.MessageBody);

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendCommentReplyNotificationAsync(x, replyPayload);
                        }, log);

                        break;

                    case "BeingPinged":
                        var pingPayload = JsonSerializer.Deserialize<PingPayload>(en.MessageBody);

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendPingNotificationAsync(x, pingPayload);
                        }, log);

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

    private async Task SendByMode(int sendingMode, EmailNotificationV3 en, Func<string[], Task> sendingAction, ILogger log)
    {
        var dl = en.DistributionList.Split(';');

        log.LogInformation($"Send to '{en.DistributionList}' using sendingMode '{sendingMode}'");

        switch (sendingMode)
        {
            case 1:
                await sendingAction(dl);
                break;
            case 2:
                {
                    // Workaround for error when sending to multiple recipients in case a part of them failed
                    // which result in other recipients also not receiving email 
                    // Fix:
                    // - Send email one by one
                    // - Log SmtpCommandException only instead of failing
                    // - Fail fast only when ALL recipients blow up

                    var exceptions = new List<SmtpCommandException>();
                    foreach (var recipient in dl)
                    {
                        try
                        {
                            log.LogInformation($"Sending to '{recipient}' using sendingMode '2'");

                            await sendingAction(new[] { recipient });
                        }
                        catch (SmtpCommandException e)
                        {
                            exceptions.Add(e);
                            log.LogError(exception: e, message: $"Error sending to '{recipient}': '{e.Message}'");
                        }
                    }

                    if (exceptions.Count == dl.Length)
                    {
                        log.LogError("Sending email all failed in sendingMode '2'");

                        // All blow up, notify Azure to retry or put message into poison queue for developers to work 996
                        throw new AggregateException("Error sending 'OpenCard' email, all messages failed with exceptions.", innerExceptions: exceptions);
                    }

                    break;
                }
        }
    }
}

public class EmailNotificationV3
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
}