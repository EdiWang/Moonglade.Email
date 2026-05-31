namespace Moonglade.Function.Email.Core;

public interface IEmailNotificationQueue
{
    string Name { get; }

    Task SendAsync(EmailNotification emailNotification);
}