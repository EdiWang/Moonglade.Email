using Azure.Storage.Queues;
using Moonglade.Function.Email.Core;
using System.Text;
using System.Text.Json;

namespace Moonglade.Function.Email;

public class StorageQueueEmailNotificationQueue(QueueClient queue) : IEmailNotificationQueue
{
    public string Name => queue.Name;

    public async Task SendAsync(EmailNotification emailNotification)
    {
        var json = JsonSerializer.Serialize(emailNotification, MoongladeJsonSerializerOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64Json = Convert.ToBase64String(bytes);

        await queue.SendMessageAsync(base64Json);
    }
}