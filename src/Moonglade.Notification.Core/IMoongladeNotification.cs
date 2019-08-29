using System;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Moonglade.Notification.Models;

namespace Moonglade.Notification.Core
{
    public interface IMoongladeNotification
    {
        Task<Response> SendTestNotificationAsync(NotificationRequest request);

        Task SendNewCommentNotificationAsync(NewCommentNotificationRequest comment);

        Task SendCommentReplyNotificationAsync(CommentReplyNotificationRequest model);

        Task SendPingNotificationAsync(PingNotificationRequest receivedPingback);
    }
}
