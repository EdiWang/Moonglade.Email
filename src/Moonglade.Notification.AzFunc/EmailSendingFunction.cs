using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Notification.Core;

namespace Moonglade.Notification.AzFunc
{
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

                var configRootDirectory = executionContext.FunctionAppDirectory;
                AppDomain.CurrentDomain.SetData(Constants.AppBaseDirectory, configRootDirectory);
                log.LogInformation($"Function App Directory: {configRootDirectory}");

                IMoongladeNotification notification = new EmailHandler(log)
                {
                    AdminEmail = request.AdminEmail,
                    EmailDisplayName = request.EmailDisplayName
                };

                switch (request.MessageType)
                {
                    case MailMesageTypes.TestMail:
                        await notification.SendTestNotificationAsync();
                        return new OkObjectResult("TestMail Sent");

                    case MailMesageTypes.NewCommentNotification:
                        var commentPayload = GetModelFromPayload<NewCommentPayload>();
                        _ = Task.Run(async () => await notification.SendNewCommentNotificationAsync(commentPayload));
                        return new OkObjectResult("NewCommentNotification Sent");

                    case MailMesageTypes.AdminReplyNotification:
                        var replyPayload = GetModelFromPayload<CommentReplyPayload>();
                        _ = Task.Run(async () => await notification.SendCommentReplyNotificationAsync(replyPayload));
                        return new OkObjectResult("AdminReplyNotification Sent");

                    case MailMesageTypes.BeingPinged:
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
}
