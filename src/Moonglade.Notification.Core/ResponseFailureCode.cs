namespace Moonglade.Notification.Core
{
    public enum ResponseFailureCode
    {
        None = 0,
        GeneralException = 1,
        InvalidParameter = 2,
        EmailSendingDisabled = 500
    }
}
