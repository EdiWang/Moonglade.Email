using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;
using System.Text.Json;

namespace TestClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var connectionString = PromptUser("Enter Azure Storage connection string:");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("Connection string cannot be empty.");
                return;
            }

            var queueName = PromptUser("Enter Queue name:");
            if (string.IsNullOrWhiteSpace(queueName) || !IsValidQueueName(queueName))
            {
                Console.WriteLine("Invalid queue name. Queue names must be 3-63 characters, lowercase letters, numbers, and hyphens only.");
                return;
            }

            var dl = PromptUser("Enter test email receiver address:");
            if (string.IsNullOrWhiteSpace(dl))
            {
                Console.WriteLine("Email address cannot be empty.");
                return;
            }

            var queueClientOptions = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64,
                Retry = { MaxRetries = 3, Delay = TimeSpan.FromSeconds(2) }
            };

            var queue = new QueueClient(connectionString, queueName, queueClientOptions);

            var testEmail = new EmailNotification
            {
                DistributionList = dl,
                MessageBody = string.Empty,
                MessageType = "TestMail"
            };

            await InsertMessageAsync(queue, testEmail);

            if (PromptUser("Retrieve next message? (Y/N)")?.ToUpper() == "Y")
            {
                var message = await RetrieveNextMessageAsync(queue);

                if (message != null)
                {
                    try
                    {
                        var decodedMessage = DecodeMessage(message);
                        var json = JsonSerializer.Serialize(decodedMessage, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($"Retrieved message:\n{json}");

                        Console.WriteLine("Press any key to delete this message");
                        Console.ReadKey();
                        await DeleteMessageAsync(queue, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("No messages found in the queue.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application error: {ex.Message}");
        }
    }

    static string PromptUser(string message)
    {
        Console.WriteLine(message);
        return Console.ReadLine()?.Trim();
    }

    static bool IsValidQueueName(string queueName)
    {
        if (queueName.Length < 3 || queueName.Length > 63)
            return false;

        return queueName.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-') &&
               !queueName.StartsWith('-') && !queueName.EndsWith('-') &&
               !queueName.Contains("--");
    }

    static async Task InsertMessageAsync(QueueClient queue, EmailNotification emailNotification)
    {
        try
        {
            var createResponse = await queue.CreateIfNotExistsAsync();
            if (createResponse != null)
            {
                Console.WriteLine("The queue was created.");
            }

            var json = JsonSerializer.Serialize(emailNotification);
            
            var response = await queue.SendMessageAsync(json);
            
            Console.WriteLine($"Message inserted successfully. MessageId: {response.Value.MessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting message: {ex.Message}");
            throw;
        }
    }

    static async Task<QueueMessage> RetrieveNextMessageAsync(QueueClient queue)
    {
        try
        {
            var existsResponse = await queue.ExistsAsync();
            if (!existsResponse.Value)
            {
                Console.WriteLine("Queue does not exist.");
                return null;
            }

            var response = await queue.ReceiveMessageAsync(TimeSpan.FromSeconds(30));
            return response.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving message: {ex.Message}");
            return null;
        }
    }

    static async Task DeleteMessageAsync(QueueClient queue, QueueMessage message)
    {
        try
        {
            var response = await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            Console.WriteLine($"Message deleted successfully. Status: {response.Status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting message: {ex.Message}");
        }
    }

    static EmailNotification DecodeMessage(QueueMessage message)
    {
        try
        {
            var messageText = message.MessageText;
            
            // Try to deserialize directly first (if using Base64 encoding option)
            try
            {
                return JsonSerializer.Deserialize<EmailNotification>(messageText);
            }
            catch
            {
                // If that fails, try Base64 decoding first
                var decodedBytes = Convert.FromBase64String(messageText);
                var decodedJson = Encoding.UTF8.GetString(decodedBytes);
                return JsonSerializer.Deserialize<EmailNotification>(decodedJson);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decoding message: {ex.Message}");
            return null;
        }
    }
}

public class EmailNotification
{
    public string DistributionList { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
}