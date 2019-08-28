namespace Moonglade.Notification.Core
{
    public class CommentReplyNotificationRequest
    {
        public string Email { get; set; }
        public string CommentContent { get; set; }
        public string Title { get; set; }
        public string ReplyContent { get; set; }
    }
}