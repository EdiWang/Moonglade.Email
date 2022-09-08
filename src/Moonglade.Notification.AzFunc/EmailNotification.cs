namespace Moonglade.Notification.AzFunc;

public class EmailNotification
{
    public Guid Id { get; set; }

    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
    public int SendingStatus { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime SentTimeUtc { get; set; }
    public string TargetResponse { get; set; }
    public int RetryCount { get; set; }
}