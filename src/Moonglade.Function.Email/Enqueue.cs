using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using System.Text;
using Newtonsoft.Json;

namespace Moonglade.Function.Email;

public static class Enqueue
{
    [FunctionName("Enqueue")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] EnqueueRequest req,
        ILogger log)
    {
        log.LogInformation($"C# HTTP trigger function `Enqueue` processed a request. OriginAspNetRequestId={req.OriginAspNetRequestId}");

        var conn = Environment.GetEnvironmentVariable("moongladestorage");
        var queue = new QueueClient(conn, "moongladeemailqueue");

        var en = new EmailNotification
        {
            DistributionList = string.Join(';', req.Receipts),
            MessageType = req.Type,

            // System.Text.Json doesn't work here because Azure Function model binding is already using Newtonsoft.Json, wtf..
            MessageBody = JsonConvert.SerializeObject(req.Payload)
        };

        await InsertMessageAsync(queue, en, log);

        return new AcceptedResult();
    }

    private static async Task InsertMessageAsync(QueueClient queue, EmailNotification emailNotification, ILogger logger)
    {
        if (null != await queue.CreateIfNotExistsAsync())
        {
            logger.LogInformation($"Azure Storage Queue '{queue.Name}' was created.");
        }

        var json = JsonConvert.SerializeObject(emailNotification);
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