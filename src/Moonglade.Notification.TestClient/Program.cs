using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;
using System.Text.Json;

namespace Moonglade.Notification.TestClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter Azure Storage connection string");
        var connectionString = Console.ReadLine();

        Console.WriteLine("Enter Queue name");
        var queueName = Console.ReadLine();

        Console.WriteLine("Enter test email receiver addresss");
        var dl = Console.ReadLine();

        var queue = new QueueClient(connectionString, queueName);

        var testEmail = new EmailNotification
        {
            DistributionList = dl,
            MessageBody = string.Empty,
            MessageType = "TestMail",
            RetryCount = 0,
            SendingStatus = 1
        };

        await InsertMessageAsync(queue, testEmail);

        Console.WriteLine("Retrive next message? (Y/N)");
        var yn = Console.ReadLine();
        if (yn.ToUpper() == "Y")
        {
            var message = await RetrieveNextMessageAsync(queue);

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);

            Console.WriteLine("Press any key to delete this message");
            Console.ReadLine();
            await DeleteMessage(queue, message);
        }
    }

    static async Task InsertMessageAsync(QueueClient theQueue, EmailNotification emailNotification)
    {
        if (null != await theQueue.CreateIfNotExistsAsync())
        {
            Console.WriteLine("The queue was created.");
        }

        var json = JsonSerializer.Serialize(emailNotification);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64Json = Convert.ToBase64String(bytes);

        await theQueue.SendMessageAsync(base64Json);

        Console.WriteLine($"Inserted message: {json}, base64: {base64Json}");
    }

    static async Task<QueueMessage> RetrieveNextMessageAsync(QueueClient theQueue)
    {
        if (await theQueue.ExistsAsync())
        {
            QueueProperties properties = await theQueue.GetPropertiesAsync();

            if (properties.ApproximateMessagesCount > 0)
            {
                QueueMessage[] retrievedMessage = await theQueue.ReceiveMessagesAsync(1);
                var theMessage = retrievedMessage[0];
                return theMessage;
            }
        }

        return null;
    }

    static async Task DeleteMessage(QueueClient theQueue, QueueMessage message)
    {
        var response = await theQueue.DeleteMessageAsync(message.MessageId, message.PopReceipt);
        Console.WriteLine($"DeleteMessage Response: {response.Status}:{response.ReasonPhrase}");
    }
}

public class EmailNotification
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
    public int SendingStatus { get; set; }
    public DateTime? SentTimeUtc { get; set; }
    public string TargetResponse { get; set; }
    public int RetryCount { get; set; }
}