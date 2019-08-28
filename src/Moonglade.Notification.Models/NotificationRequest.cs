﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Moonglade.Notification.Models
{
    public class NotificationRequest
    {
        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; }

        [Required]
        public string EmailDisplayName { get; set; }
    }
}