using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;
using System.Text.Json;

namespace TestClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = PromptUser("Enter Azure Storage connection string:");
        var queueName = PromptUser("Enter Queue name:");
        var dl = PromptUser("Enter test email receiver address:");

        var queue = new QueueClient(connectionString, queueName);

        var testEmail = new EmailNotification
        {
            DistributionList = dl,
            MessageBody = string.Empty,
            MessageType = "TestMail"
        };

        await InsertMessageAsync(queue, testEmail);

        if (PromptUser("Retrieve next message? (Y/N)").ToUpper() == "Y")
        {
            var message = await RetrieveNextMessageAsync(queue);

            if (message != null)
            {
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);

                Console.WriteLine("Press any key to delete this message");
                Console.ReadLine();
                await DeleteMessageAsync(queue, message);
            }
            else
            {
                Console.WriteLine("No messages found in the queue.");
            }
        }
    }

    static string PromptUser(string message)
    {
        Console.WriteLine(message);
        return Console.ReadLine();
    }

    static async Task InsertMessageAsync(QueueClient queue, EmailNotification emailNotification)
    {
        if (await queue.CreateIfNotExistsAsync() != null)
        {
            Console.WriteLine("The queue was created.");
        }

        var json = JsonSerializer.Serialize(emailNotification);
        var base64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        await queue.SendMessageAsync(base64Json);

        Console.WriteLine($"Inserted message: {json}, base64: {base64Json}");
    }

    static async Task<QueueMessage> RetrieveNextMessageAsync(QueueClient queue)
    {
        if (await queue.ExistsAsync())
        {
            QueueProperties properties = await queue.GetPropertiesAsync();

            if (properties.ApproximateMessagesCount > 0)
            {
                QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1);
                var theMessage = retrievedMessage[0];
                return theMessage;
            }
        }

        return null;
    }

    static async Task DeleteMessageAsync(QueueClient queue, QueueMessage message)
    {
        if (message != null)
        {
            var response = await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            Console.WriteLine($"DeleteMessage Response: {response.Status}:{response.ReasonPhrase}");
        }
    }
}

public class EmailNotification
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
}