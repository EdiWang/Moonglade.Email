using Azure.Storage.Queues.Models;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
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
        [QueueTrigger(Enqueue.QueueName, Connection = "MOONGLADE_EMAIL_STORAGE")] QueueMessage queueMessage)
    {
        logger.LogInformation("Queue trigger function processed: {MessageId}", queueMessage.MessageId);

        try
        {
            var en = JsonSerializer.Deserialize<EmailNotification>(queueMessage.MessageText);

            var contractErrors = EmailNotificationContract.ValidateNotification(en);

            if (contractErrors.Length > 0)
            {
                logger.LogWarning("Message {MessageId} contract validation failed: {Errors}",
                    queueMessage.MessageId, string.Join(", ", contractErrors));
                return;
            }

            var payloadErrors = EmailNotificationContract.ValidatePayload(en.MessageType, en.MessageBody);
            if (payloadErrors.Length > 0)
            {
                logger.LogWarning("Message {MessageId} payload validation failed: {Errors}",
                    queueMessage.MessageId, string.Join(", ", payloadErrors));
                return;
            }

            logger.LogInformation("Found message: {MessageId}", queueMessage.MessageId);

            logger.LogInformation("Sending {MessageType} message", en.MessageType);

            await SendMessage(en);

            logger.LogInformation("Message {MessageId} processing completed.", queueMessage.MessageId);
        }
        catch (JsonException e)
        {
            logger.LogWarning(e, "Queue message {MessageId} is not valid JSON, skipping.", queueMessage.MessageId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing queue message {MessageId}", queueMessage.MessageId);
            throw;
        }
    }

    private async Task SendMessage(EmailNotification en)
    {
        var recipients = EmailNotificationContract.ParseDistributionList(en.DistributionList);

        // Send one by one so a permanent failure for one recipient does not block the rest.

        var failures = new List<EmailDeliveryFailure>();
        foreach (var recipient in recipients)
        {
            try
            {
                logger.LogInformation("Sending to {Recipient}", recipient);

                var message = GetMessage(en.MessageType, [recipient], en.MessageBody);
                await dispatcher.SendAsync(message);
            }
            catch (Exception e) when (EmailDeliveryFailureClassifier.TryClassify(e, out _))
            {
                EmailDeliveryFailureClassifier.TryClassify(e, out var kind);
                failures.Add(new EmailDeliveryFailure(recipient, kind, e));
                logger.LogError(e, "{FailureKind} error sending {MessageType} message to {Recipient}",
                    kind, en.MessageType, recipient);
            }
        }

        if (failures.Count == 0)
        {
            return;
        }

        var permanentFailures = failures.Count(f => f.Kind == EmailDeliveryFailureKind.Permanent);
        var transientFailures = failures.Count(f => f.Kind == EmailDeliveryFailureKind.Transient);
        logger.LogWarning(
            "Message {MessageType} send completed with {PermanentFailureCount} permanent failures and {TransientFailureCount} transient failures out of {RecipientCount} recipients.",
            en.MessageType,
            permanentFailures,
            transientFailures,
            recipients.Length);

        if (failures.Count != recipients.Length)
        {
            return;
        }

        if (transientFailures > 0)
        {
            logger.LogError(
                "All {RecipientCount} email recipients failed and {TransientFailureCount} failures are retryable.",
                recipients.Length,
                transientFailures);

            throw new AggregateException(
                "All email recipients failed to receive the message, and at least one failure is retryable.",
                innerExceptions: failures.Select(f => f.Exception));
        }

        logger.LogWarning(
            "All {RecipientCount} email recipients failed permanently; message will not be retried.",
            recipients.Length);
    }

    private CommonMailMessage GetMessage(string messageType, string[] recipients, string messageBody)
    {
        switch (messageType)
        {
            case MessageTypes.TestMail:
                return messageBuilder.BuildTestNotification(recipients, smtpSettings);

            case MessageTypes.NewCommentNotification:
                var ncPayload = JsonSerializer.Deserialize<NewCommentPayload>(messageBody, MoongladeJsonSerializerOptions.Default);
                return messageBuilder.BuildNewCommentNotification(recipients, ncPayload);

            case MessageTypes.AdminReplyNotification:
                var replyPayload = JsonSerializer.Deserialize<CommentReplyPayload>(messageBody, MoongladeJsonSerializerOptions.Default);
                return messageBuilder.BuildCommentReplyNotification(recipients, replyPayload);

            case MessageTypes.BeingPinged:
                var pingPayload = JsonSerializer.Deserialize<PingPayload>(messageBody, MoongladeJsonSerializerOptions.Default);
                return messageBuilder.BuildPingNotification(recipients, pingPayload);

            default:
                throw new ArgumentOutOfRangeException(nameof(messageType), messageType, "Unsupported message type.");
        }
    }
}
