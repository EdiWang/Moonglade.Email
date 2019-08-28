using System.ComponentModel.DataAnnotations;

namespace Moonglade.Notification.Models
{
    public class CommentReplyNotificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string CommentContent { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string ReplyContent { get; set; }

        [Required]
        public string PostLink { get; set; }
    }
}