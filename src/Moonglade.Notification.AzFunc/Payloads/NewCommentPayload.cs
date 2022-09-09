using System.ComponentModel.DataAnnotations;

namespace Moonglade.Notification.AzFunc.Payloads;

public class NewCommentPayload
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