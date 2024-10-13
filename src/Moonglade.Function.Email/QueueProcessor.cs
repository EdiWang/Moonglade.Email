using Azure.Storage.Queues.Models;
using Edi.TemplateEmail;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
            var en = JsonSerializer.Deserialize<EmailNotification>(queueMessage.MessageText);

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

                var messageBuilder = new MessageBuilder(emailHelper);
                logger.LogInformation($"Sending {en.MessageType} message");

                var sendingMode = 1;
                var envSendingMode = EnvHelper.Get<int>("DistributionListSendingMode");
                if (envSendingMode != 0) sendingMode = envSendingMode;

                await SendMessage(sendingMode, en, messageBuilder);

                logger.LogInformation($"Message '{queueMessage.MessageId}' processed successfully.");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    private async Task SendMessage(int sendingMode, EmailNotification en, MessageBuilder builder)
    {
        var dl = en.DistributionList.Split(';');

        switch (sendingMode)
        {
            case 1:
                logger.LogInformation($"Sending to '{en.DistributionList}' using sendingMode '1'");
                var message1 = GetMessage(en.MessageType, dl, en.MessageBody, builder);
                await message1.SendAsync();

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

                                var message2 = GetMessage(en.MessageType, [recipient], en.MessageBody, builder);
                                await message2.SendAsync();
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

    private CommonMailMessage GetMessage(string messageType, string[] recipients, string messageBody,
        MessageBuilder builder)
    {
        switch (messageType)
        {
            case "TestMail":
                return builder.BuildTestNotification(recipients);

            case "NewCommentNotification":
                var ncPayload = JsonSerializer.Deserialize<NewCommentPayload>(messageBody);
                return builder.BuildNewCommentNotification(recipients, ncPayload);

            case "AdminReplyNotification":
                var replyPayload = JsonSerializer.Deserialize<CommentReplyPayload>(messageBody);
                return builder.BuildCommentReplyNotification(recipients, replyPayload);

            case "BeingPinged":
                var pingPayload = JsonSerializer.Deserialize<PingPayload>(messageBody);
                return builder.BuildPingNotification(recipients, pingPayload);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
