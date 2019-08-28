using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Moonglade.Notification.Models
{
    public class NewCommentNotificationRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string IpAddress { get; set; }

        [Required]
        public string PostTitle { get; set; }

        [Required]
        public string CommentContent { get; set; }

        [Required]
        public DateTime CreateOnUtc { get; set; }
    }
}
