using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moonglade.Function.Email.Core;
using System.Text;
using System.Text.Json;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Moonglade.Function.Email;

public class Enqueue(ILogger<Enqueue> logger)
{
    [Function("Enqueue")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [FromBody] EnqueueRequest payload)
    {
        logger.LogInformation($"C# HTTP trigger function `Enqueue` processed a request. OriginAspNetRequestId={payload.OriginAspNetRequestId}");

        var conn = Environment.GetEnvironmentVariable("moongladestorage");
        var queue = new QueueClient(conn, "moongladeemailqueue");

        var en = new EmailNotification
        {
            DistributionList = string.Join(';', payload.Receipts),
            MessageType = payload.Type,
            MessageBody = JsonSerializer.Serialize(payload.Payload, MoongladeJsonSerializerOptions.Default)
        };

        await InsertMessageAsync(queue, en);

        return new AcceptedResult();
    }

    private async Task InsertMessageAsync(QueueClient queue, EmailNotification emailNotification)
    {
        if (null != await queue.CreateIfNotExistsAsync())
        {
            logger.LogInformation($"Azure Storage Queue '{queue.Name}' was created.");
        }

        var json = JsonSerializer.Serialize(emailNotification, MoongladeJsonSerializerOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64Json = Convert.ToBase64String(bytes);

        await queue.SendMessageAsync(base64Json);
    }
}

public class EnqueueRequest
{
    public string Type { get; set; }
    public string[] Receipts { get; set; }
    public object Payload { get; set; }
    public string OriginAspNetRequestId { get; set; } = Guid.Empty.ToString();
}