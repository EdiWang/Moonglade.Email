using Dapper;
using Edi.TemplateEmail;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moonglade.Notification.AzFunc.Core;
using Moonglade.Notification.AzFunc.Payloads;
using System.Text.Json;

namespace Moonglade.Notification.AzFunc;

public class NotificationV2
{
    [FunctionName("NotificationV2")]
    public async Task Run([TimerTrigger("* * * * *")] TimerInfo myTimer,
        ILogger log,
        Microsoft.Azure.WebJobs.ExecutionContext executionContext)
    {
        log.LogInformation($"NotificationV2 Timer trigger function executed at UTC: {DateTime.UtcNow}");

        var str = Environment.GetEnvironmentVariable("moongladedb_connection");
        if (string.IsNullOrWhiteSpace(str))
        {
            var message = "Failed to get `moongladedb_connection`.";
            log.LogError(message);
            throw new ArgumentNullException("moongladedb_connection", message);
        }

        await using var conn = new SqlConnection(str);
        conn.Open();

        var sql = "SELECT TOP 1 * FROM EmailNotification en " +
                  "WHERE en.SendingStatus = 1 " +
                  "OR en.SendingStatus = 8 " +
                  "AND en.RetryCount < 5 " +
                  "ORDER BY CreateTimeUtc";

        try
        {
            var en = await conn.QueryFirstOrDefaultAsync<EmailNotification>(sql);

            if (en != null)
            {
                log.LogInformation($"Found message: {en.Id}");

                if (string.IsNullOrWhiteSpace(en.DistributionList))
                {
                    log.LogError($"Message Id '{en.Id}' has no DistributionList, operation aborted.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(en.MessageType))
                {
                    log.LogError($"Message Id '{en.Id}' has no MessageType, operation aborted.");
                    return;
                }

                // Set status to '2 - InProgress'
                var sqlUpdateInProgress = "UPDATE EmailNotification SET SendingStatus = 2 WHERE Id = @Id";
                await conn.ExecuteAsync(sqlUpdateInProgress, new { en.Id });
                log.LogInformation($"Set message: {en.Id} to '2 - InProgress'");

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

                        if (eventArgs.IsSuccess)
                        {
                            // Set status to '4 - Sent'
                            await using var conn2 = new SqlConnection(str);
                            var sqlUpdateSent = "UPDATE EmailNotification SET SendingStatus = 4, SentTimeUtc = @SentTimeUtc WHERE Id = @Id";
                            await conn2.ExecuteAsync(sqlUpdateSent, new { en.Id, SentTimeUtc = DateTime.UtcNow });
                            log.LogInformation($"Set message: {en.Id} to '4 - Sent'");
                        }
                        else
                        {
                            // Set status to '8 - Failed'
                            await using var conn2 = new SqlConnection(str);
                            var sqlUpdateFailed =
                                "UPDATE EmailNotification SET SendingStatus = 8, RetryCount = RetryCount + 1 WHERE Id = @Id";
                            await conn2.ExecuteAsync(sqlUpdateFailed, new { en.Id });
                            log.LogWarning($"Set message: {en.Id} to '8 - Failed'");
                        }
                    }
                };

                emailHelper.EmailFailed += (sender, args) =>
                {
                    if (sender is MimeMessage msg)
                    {
                        log.LogError($"Email '{msg.Subject}' failed: {args.ServerResponse}");
                    }
                };

                var dName = Environment.GetEnvironmentVariable("SenderDisplayName");
                var notification = new EmailHandler(emailHelper, dName);
                log.LogInformation($"Sending {en.MessageType} message");

                try
                {
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
                catch (Exception e)
                {
                    log.LogError(e.Message);

                    // Set status to '8 - Failed'
                    var sqlUpdateFailed =
                        "UPDATE EmailNotification SET SendingStatus = 8, RetryCount = RetryCount + 1 WHERE Id = @Id";
                    await conn.ExecuteAsync(sqlUpdateFailed, new { en.Id });
                    log.LogWarning($"Set message: {en.Id} to '8 - Failed'");
                }
            }
            else
            {
                log.LogInformation("No messages found, waiting for next run");
            }
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            throw;
        }
    }
}

public class EmailNotification
{
    public Guid Id { get; set; }

    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
    public int SendingStatus { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime SentTimeUtc { get; set; }
    public string TargetResponse { get; set; }
    public int RetryCount { get; set; }
}