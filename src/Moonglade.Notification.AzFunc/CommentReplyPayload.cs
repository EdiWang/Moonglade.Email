using System.ComponentModel.DataAnnotations;

namespace Moonglade.Notification.AzFunc;

public class CommentReplyPayload
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string CommentContent { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string ReplyContentHtml { get; set; }

    [Required]
    public string PostLink { get; set; }
}