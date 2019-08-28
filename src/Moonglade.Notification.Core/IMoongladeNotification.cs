using System;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Moonglade.Notification.Models;

namespace Moonglade.Notification.Core
{
    public interface IMoongladeNotification
    {
        bool IsEnabled { get; set; }

        Task<Response> SendTestNotificationAsync();

        Task SendNewCommentNotificationAsync(NewCommentNotificationRequest comment, Func<string, string> funcCommentContentFormat);

        Task SendCommentReplyNotificationAsync(CommentReplyNotificationRequest model, string postLink);

        Task SendPingNotificationAsync(PingNotificationRequest receivedPingback);
    }
}
