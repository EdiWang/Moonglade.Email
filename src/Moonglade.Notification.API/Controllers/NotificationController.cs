using System;
using System.Text.Json;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Notification.Core;
using Moonglade.Notification.Models;

namespace Moonglade.Notification.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;

        private readonly IMoongladeNotification _notification;

        public NotificationController(ILogger<NotificationController> logger, IMoongladeNotification notification)
        {
            _logger = logger;
            _notification = notification;
        }

        [AllowAnonymous]
        [HttpGet]
        public string Get()
        {
            return $"Moonglade.Notification.API Version {Utils.AppVersion}";
        }

        [HttpPost]
        public async Task<Response> Post(NotificationRequest request)
        {
            T GetModelFromPayload<T>() where T : class
            {
                var json = request.Payload.ToString();
                return JsonSerializer.Deserialize<T>(json);
            }

            try
            {
                _notification.AdminEmail = request.AdminEmail;
                _notification.EmailDisplayName = request.EmailDisplayName;
                switch (request.MessageType)
                {
                    case MailMesageTypes.TestMail:
                        await _notification.SendTestNotificationAsync();
                        return new SuccessResponse();

                    case MailMesageTypes.NewCommentNotification:
                        var commentPayload = GetModelFromPayload<NewCommentPayload>();
                        _ = Task.Run(async () => await _notification.SendNewCommentNotificationAsync(commentPayload));
                        return new SuccessResponse();

                    case MailMesageTypes.AdminReplyNotification:
                        var replyPayload = GetModelFromPayload<CommentReplyPayload>();
                        _ = Task.Run(async () => await _notification.SendCommentReplyNotificationAsync(replyPayload));
                        return new SuccessResponse();

                    case MailMesageTypes.BeingPinged:
                        var pingPayload = GetModelFromPayload<PingPayload>();
                        _ = Task.Run(async () => await _notification.SendPingNotificationAsync(pingPayload));
                        return new SuccessResponse();

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error sending notification for type '{request.MessageType}'. Requested by '{User.Identity.Name}'");
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }
    }
}