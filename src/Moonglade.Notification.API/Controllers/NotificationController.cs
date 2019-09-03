using System;
using System.Runtime.CompilerServices;
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
            try
            {
                switch (request.MessageType)
                {
                    case MailMesageTypes.TestMail:
                        var result = await _notification.SendTestNotificationAsync(request);
                        if (!result.IsSuccess)
                        {
                            Response.StatusCode = StatusCodes.Status500InternalServerError;
                        }
                        return result;
                    case MailMesageTypes.NewCommentNotification:
                        // use automapper to convert this?
                        var model = (NewCommentNotificationRequest) request.Payload;
                        _ = Task.Run(async () => await _notification.SendNewCommentNotificationAsync(model));
                        return new SuccessResponse();
                    case MailMesageTypes.AdminReplyNotification:
                        _ = Task.Run(async () => await _notification.SendCommentReplyNotificationAsync(
                            request.Payload as CommentReplyNotificationRequest));
                        return new SuccessResponse();
                    case MailMesageTypes.BeingPinged:
                        _ = Task.Run(async () => await _notification.SendPingNotificationAsync(
                            request.Payload as PingNotificationRequest));
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