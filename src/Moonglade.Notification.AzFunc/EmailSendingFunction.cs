using System.Text.Json;
using Edi.TemplateEmail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Moonglade.Notification.AzFunc;

public class EmailSendingFunction
{
    [FunctionName("EmailSending")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] NotificationRequest request,
        ILogger log, ExecutionContext executionContext)
    {
        T GetModelFromPayload<T>() where T : class
        {
            var json = request.Payload.ToString();
            return JsonSerializer.Deserialize<T>(json);
        }

        log.LogInformation("EmailSending HTTP trigger function processed a request.");

        try
        {
            var configSource = Path.Join(executionContext.FunctionAppDirectory, "mailConfiguration.xml");
            if (!File.Exists(configSource)) throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

            var emailHelper = new EmailHelper(
                configSource,
                Environment.GetEnvironmentVariable("SmtpServer"),
                Environment.GetEnvironmentVariable("SmtpUserName"),
                Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
                int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "587"));

            if (bool.Parse(Environment.GetEnvironmentVariable("EnableSsl") ?? "true")) emailHelper.WithTls();

            emailHelper.EmailSent += (sender, eventArgs) =>
            {
                if (sender is MimeMessage msg)
                {
                    log.LogInformation($"Email '{msg.Subject}' is sent. Success: {eventArgs.IsSuccess}");
                }
            };

            var notification = new EmailHandler(emailHelper, request.EmailDisplayName, request.AdminEmail);
            log.LogInformation($"Sending {request.MessageType} message");

            switch (request.MessageType)
            {
                case MailMesageTypes.TestMail:
                    await notification.SendTestNotificationAsync();
                    break;

                case MailMesageTypes.NewCommentNotification:
                    var commentPayload = GetModelFromPayload<NewCommentPayload>();
                    _ = Task.Run(async () => await notification.SendNewCommentNotificationAsync(commentPayload));
                    break;

                case MailMesageTypes.AdminReplyNotification:
                    var replyPayload = GetModelFromPayload<CommentReplyPayload>();
                    _ = Task.Run(async () => await notification.SendCommentReplyNotificationAsync(replyPayload));
                    break;

                case MailMesageTypes.BeingPinged:
                    var pingPayload = GetModelFromPayload<PingPayload>();
                    _ = Task.Run(async () => await notification.SendPingNotificationAsync(pingPayload));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new OkObjectResult($"{request.MessageType} Sent");

        }
        catch (Exception e)
        {
            log.LogError(e, e.Message);
            return new ConflictObjectResult(e.Message);
        }
    }
}