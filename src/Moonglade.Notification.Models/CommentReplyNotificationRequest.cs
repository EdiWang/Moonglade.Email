namespace Moonglade.Notification.Models
{
    public class CommentReplyNotificationRequest
    {
        public string Email { get; set; }
        public string CommentContent { get; set; }
        public string Title { get; set; }
        public string ReplyContent { get; set; }
        public string PostLink { get; set; }
    }
}