using Azure.Storage.Queues.Models;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moonglade.Function.Email.Core;
using Moonglade.Function.Email.Payloads;
using System.Text.Json;

namespace Moonglade.Function.Email;

public class QueueProcessor(ILogger<QueueProcessor> logger)
{
    [Function("QueueProcessor")]
    public async Task Run(
        [QueueTrigger("moongladeemailqueue", Connection = "moongladestorage")] QueueMessage queueMessage)
    {
        logger.LogInformation($"C# Queue trigger function processed: {queueMessage.MessageId}");

        try
        {
            var en = JsonSerializer.Deserialize<EmailNotificationV3>(queueMessage.MessageText);

            if (en != null)
            {
                logger.LogInformation($"Found message: {queueMessage.MessageId}");

                if (string.IsNullOrWhiteSpace(en.DistributionList))
                {
                    logger.LogError($"Message Id '{queueMessage.MessageId}' has no DistributionList, operation aborted.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(en.MessageType))
                {
                    logger.LogError($"Message Id '{queueMessage.MessageId}' has no MessageType, operation aborted.");
                    return;
                }

                var runningDirectory = Environment.CurrentDirectory;
                var emailHelper = Helper.GetEmailHelper(runningDirectory);

                emailHelper.EmailSent += (sender, eventArgs) =>
                {
                    if (sender is MimeMessage msg)
                    {
                        logger.LogInformation($"Email '{msg.Subject}' is sent. Success: {eventArgs.IsSuccess}");
                    }
                };

                emailHelper.EmailFailed += (sender, args) =>
                {
                    if (sender is MimeMessage msg)
                    {
                        logger.LogError($"Email '{msg.Subject}' failed: {args.ServerResponse}");
                        throw new(args.ServerResponse);
                    }
                };

                var dName = Environment.GetEnvironmentVariable("SenderDisplayName");
                var notification = new EmailHandler(emailHelper, dName);
                logger.LogInformation($"Sending {en.MessageType} message");

                var sendingMode = 1;
                var envSendingMode = Environment.GetEnvironmentVariable("DistributionListSendingMode");
                if (!string.IsNullOrWhiteSpace(envSendingMode))
                {
                    bool isParsed = int.TryParse(envSendingMode, out sendingMode);
                    if (!isParsed)
                    {
                        logger.LogWarning("Failed to parse 'DistributionListSendingMode', falling back to '1', please check settings.");
                    }
                }

                switch (en.MessageType)
                {
                    case "TestMail":
                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendTestNotificationAsync(x);
                        });

                        break;

                    case "NewCommentNotification":
                        var ncPayload = JsonSerializer.Deserialize<NewCommentPayload>(en.MessageBody);

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendNewCommentNotificationAsync(x, ncPayload);
                        });

                        break;

                    case "AdminReplyNotification":
                        var replyPayload = JsonSerializer.Deserialize<CommentReplyPayload>(en.MessageBody);

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendCommentReplyNotificationAsync(x, replyPayload);
                        });

                        break;

                    case "BeingPinged":
                        var pingPayload = JsonSerializer.Deserialize<PingPayload>(en.MessageBody);

                        await SendByMode(sendingMode, en, async (x) =>
                        {
                            await notification.SendPingNotificationAsync(x, pingPayload);
                        });

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    private async Task SendByMode(int sendingMode, EmailNotificationV3 en, Func<string[], Task> sendingAction)
    {
        var dl = en.DistributionList.Split(';');

        switch (sendingMode)
        {
            case 1:
                logger.LogInformation($"Sending to '{en.DistributionList}' using sendingMode '1'");

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
                            if (!string.IsNullOrWhiteSpace(recipient))
                            {
                                logger.LogInformation($"Sending to '{recipient}' using sendingMode '2'");
                                await sendingAction(new[] { recipient });
                            }
                        }
                        catch (SmtpCommandException e)
                        {
                            exceptions.Add(e);
                            logger.LogError(exception: e, message: $"Error sending to '{recipient}': '{e.Message}'");
                        }
                    }

                    if (exceptions.Count == dl.Length)
                    {
                        logger.LogError("Sending email all failed in sendingMode '2'");

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