using System.ComponentModel.DataAnnotations;

namespace Moonglade.Notification.AzFunc.Payloads;

public class PingPayload
{
    [Required]
    public string TargetPostTitle { get; set; }

    [Required]
    public string Domain { get; set; }

    [Required]
    public string SourceIp { get; set; }

    [Required]
    public string SourceUrl { get; set; }

    [Required]
    public string SourceTitle { get; set; }
}