using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Moonglade.Notification.Models
{
    public class NotificationRequest
    {
        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; }

        [Required]
        public string EmailDisplayName { get; set; }

        [Required]
        public MailMesageTypes MessageType { get; set; }

        public object Payload { get; set; }
    }
}