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

        private async Task<Response> TryExecuteAsync(
            Func<Task<Response>> func, [CallerMemberName] string callerMemberName = "", object keyParameter = null)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error executing {callerMemberName}({keyParameter}). Requested by '{User.Identity.Name}'");
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public string Get()
        {
            return $"Moonglade.Notification.API Version {Utils.AppVersion}";
        }

        [HttpPost]
        [Route("test")]
        public async Task<Response> SendTestNotification(NotificationRequest request)
        {
            var result = await _notification.SendTestNotificationAsync(request);
            if (!result.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            return result;
        }

        [HttpPost]
        [Route("newcomment")]
        public async Task<Response> SendNewCommentNotification(NewCommentNotificationRequest comment)
        {
            return await TryExecuteAsync(async () =>
            {
                await _notification.SendNewCommentNotificationAsync(comment);
                return new SuccessResponse();
            });
        }

        [HttpPost]
        [Route("commentreply")]
        public async Task<Response> SendCommentReplyNotification(CommentReplyNotificationRequest commentReply)
        {
            return await TryExecuteAsync(async () =>
            {
                await _notification.SendCommentReplyNotificationAsync(commentReply);
                return new SuccessResponse();
            });
        }

        [HttpPost]
        [Route("ping")]
        public async Task<Response> SendPingNotification(PingNotificationRequest receivedPingback)
        {
            return await TryExecuteAsync(async () =>
            {
                await _notification.SendPingNotificationAsync(receivedPingback);
                return new SuccessResponse();
            });
        }
    }
}