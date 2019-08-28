using System;

namespace Moonglade.Notification.Models
{
    public class PingNotificationRequest
    {
        public string TargetPostTitle { get; set; }
        public DateTime PingTimeUtc { get; set; }
        public string Domain { get; set; }
        public string SourceIp { get; set; }
        public string SourceUrl { get; set; }
        public string SourceTitle { get; set; }
    }
}