using System.ComponentModel.DataAnnotations;

namespace Moonglade.Notification.Core;

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