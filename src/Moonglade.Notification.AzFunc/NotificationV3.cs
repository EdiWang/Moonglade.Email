using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Moonglade.Notification.AzFunc;

public class NotificationV3
{
    [FunctionName("NotificationV3")]
    public void Run([QueueTrigger("moongladeemailqueue", Connection = "moongladestorage")] QueueMessage queueMessage, ILogger log)
    {
        log.LogInformation($"C# Queue trigger function processed: {queueMessage.MessageId}");

        try
        {
            var en = JsonSerializer.Deserialize<EmailNotificationV3>(queueMessage.MessageText);
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            throw;
        }
    }
}

public class EmailNotificationV3
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
    public int SendingStatus { get; set; }
    public DateTime? SentTimeUtc { get; set; }
    public string TargetResponse { get; set; }
    public int RetryCount { get; set; }
}