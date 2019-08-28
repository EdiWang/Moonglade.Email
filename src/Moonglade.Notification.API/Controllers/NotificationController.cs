using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Notification.Core;

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
            return "Hello";
        }

        [HttpPost]
        [Route("test")]
        public async Task<Response> SendTestNotification()
        {
            return await _notification.SendTestNotificationAsync();
        }
    }
}