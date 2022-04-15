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

            var notification = new EmailHandler(emailHelper, request.AdminEmail, request.EmailDisplayName);

            switch (request.MessageType)
            {
                case MailMesageTypes.TestMail:
                    log.LogInformation("Sending test mail");
                    await notification.SendTestNotificationAsync();
                    return new OkObjectResult("TestMail Sent");

                case MailMesageTypes.NewCommentNotification:
                    log.LogInformation("Sending NewCommentNotification mail");
                    var commentPayload = GetModelFromPayload<NewCommentPayload>();
                    _ = Task.Run(async () => await notification.SendNewCommentNotificationAsync(commentPayload));
                    return new OkObjectResult("NewCommentNotification Sent");

                case MailMesageTypes.AdminReplyNotification:
                    log.LogInformation("Sending AdminReplyNotification mail");
                    var replyPayload = GetModelFromPayload<CommentReplyPayload>();
                    _ = Task.Run(async () => await notification.SendCommentReplyNotificationAsync(replyPayload));
                    return new OkObjectResult("AdminReplyNotification Sent");

                case MailMesageTypes.BeingPinged:
                    log.LogInformation($"Sending BeingPinged mail");
                    var pingPayload = GetModelFromPayload<PingPayload>();
                    _ = Task.Run(async () => await notification.SendPingNotificationAsync(pingPayload));
                    return new OkObjectResult("BeingPinged Sent");

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception e)
        {
            log.LogError(e, e.Message);
            return new ConflictObjectResult(e.Message);
        }
    }
}