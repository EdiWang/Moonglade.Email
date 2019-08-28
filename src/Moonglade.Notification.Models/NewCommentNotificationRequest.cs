using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Notification.Models
{
    public class NewCommentNotificationRequest
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public string IpAddress { get; set; }

        public string PostTitle { get; set; }

        public string CommentContent { get; set; }

        public DateTime CreateOnUtc { get; set; }
    }
}
