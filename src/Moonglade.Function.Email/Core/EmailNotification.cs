namespace Moonglade.Function.Email.Core;

internal record EmailNotification
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
}