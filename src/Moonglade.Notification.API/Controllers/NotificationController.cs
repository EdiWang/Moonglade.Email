using System;
using System.Collections.Generic;
using System.Linq;
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

        [HttpGet]
        public string Get()
        {
            return $"Moonglade.Notification.API Version {Utils.AppVersion}";
        }

        [Authorize]
        [HttpPost]
        [Route("test")]
        public async Task<Response> SendTestNotification()
        {
            var result = await _notification.SendTestNotificationAsync();
            if (!result.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            return result;
        }

        [Authorize]
        [HttpPost]
        [Route("newcomment")]
        public async Task<Response> SendNewCommentNotification(NewCommentNotificationRequest comment)
        {
            try
            {
                // TODO: Validate parameters

                await _notification.SendNewCommentNotificationAsync(comment, Utils.MdContentToHtml);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(SendNewCommentNotification));
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        [Authorize]
        [HttpPost]
        [Route("commentreply")]
        public async Task<Response> SendCommentReplyNotification(CommentReplyNotificationRequest commentReply)
        {
            try
            {
                // TODO: Validate parameters

                await _notification.SendCommentReplyNotificationAsync(commentReply);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(SendCommentReplyNotification));
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        [Authorize]
        [HttpPost]
        [Route("commentreply")]
        public async Task<Response> SendPingNotification(PingNotificationRequest receivedPingback)
        {
            try
            {
                // TODO: Validate parameters

                await _notification.SendPingNotificationAsync(receivedPingback);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(SendPingNotification));
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }
    }
}