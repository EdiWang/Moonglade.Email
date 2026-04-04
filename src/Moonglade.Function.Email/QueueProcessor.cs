using Azure.Storage.Queues.Models;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moonglade.Function.Email.Core;
using Moonglade.Function.Email.Payloads;
using System.Text.Json;

namespace Moonglade.Function.Email;

public class QueueProcessor(ILogger<QueueProcessor> logger, MessageBuilder messageBuilder, EmailSettings smtpSettings, IEmailDispatcher dispatcher)
{
    [Function("QueueProcessor")]
    public async Task Run(
        [QueueTrigger("moongladeemailqueue", Connection = "MOONGLADE_EMAIL_STORAGE")] QueueMessage queueMessage)
    {
        logger.LogInformation("Queue trigger function processed: {MessageId}", queueMessage.MessageId);

        try
        {
            var en = JsonSerializer.Deserialize<EmailNotification>(queueMessage.MessageText);

            if (en != null)
            {
                logger.LogInformation("Found message: {MessageId}", queueMessage.MessageId);

                if (string.IsNullOrWhiteSpace(en.DistributionList))
                {
                    logger.LogError("Message {MessageId} has no DistributionList, operation aborted.", queueMessage.MessageId);
                    return;
                }

                if (string.IsNullOrWhiteSpace(en.MessageType))
                {
                    logger.LogError("Message {MessageId} has no MessageType, operation aborted.", queueMessage.MessageId);
                    return;
                }

                logger.LogInformation("Sending {MessageType} message", en.MessageType);

                await SendMessage(en);

                logger.LogInformation("Message {MessageId} processed successfully.", queueMessage.MessageId);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing queue message {MessageId}", queueMessage.MessageId);
            throw;
        }
    }

    private async Task SendMessage(EmailNotification en)
    {
        var recipients = en.DistributionList.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Workaround for error when sending to multiple recipients in case a part of them failed
        // which result in other recipients also not receiving email 
        // Fix:
        // - Send email one by one
        // - Log SmtpCommandException only instead of failing
        // - Fail fast only when ALL recipients blow up

        var exceptions = new List<SmtpCommandException>();
        foreach (var recipient in recipients)
        {
            try
            {
                logger.LogInformation("Sending to {Recipient}", recipient);

                var message = GetMessage(en.MessageType, [recipient], en.MessageBody);
                await dispatcher.SendAsync(message);
            }
            catch (SmtpCommandException e)
            {
                exceptions.Add(e);
                logger.LogError(e, "Error sending to {Recipient}", recipient);
            }
        }

        if (exceptions.Count == recipients.Length)
        {
            logger.LogError("All {RecipientCount} email recipients failed", recipients.Length);

            // All blow up, notify Azure to retry or put message into poison queue
            throw new AggregateException("All email recipients failed to receive the message.", innerExceptions: exceptions);
        }
    }

    private CommonMailMessage GetMessage(string messageType, string[] recipients, string messageBody)
    {
        switch (messageType)
        {
            case MessageTypes.TestMail:
                return messageBuilder.BuildTestNotification(recipients, smtpSettings);

            case MessageTypes.NewCommentNotification:
                var ncPayload = JsonSerializer.Deserialize<NewCommentPayload>(messageBody, options);
                return messageBuilder.BuildNewCommentNotification(recipients, ncPayload);

            case MessageTypes.AdminReplyNotification:
                var replyPayload = JsonSerializer.Deserialize<CommentReplyPayload>(messageBody, options);
                return messageBuilder.BuildCommentReplyNotification(recipients, replyPayload);

            case MessageTypes.BeingPinged:
                var pingPayload = JsonSerializer.Deserialize<PingPayload>(messageBody, options);
                return messageBuilder.BuildPingNotification(recipients, pingPayload);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true
    };

}
