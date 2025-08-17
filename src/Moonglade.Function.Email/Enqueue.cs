using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moonglade.Function.Email.Core;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Moonglade.Function.Email;

public class Enqueue(ILogger<Enqueue> logger)
{
    private const string QueueName = "moongladeemailqueue";

    [Function("Enqueue")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [FromBody] EnqueueRequest payload)
    {
        try
        {
            logger.LogInformation("Processing email enqueue request. OriginAspNetRequestId={OriginAspNetRequestId}", 
                payload?.OriginAspNetRequestId);

            // Validate payload
            if (payload == null)
            {
                logger.LogWarning("Received null payload");
                return new BadRequestObjectResult("Request payload cannot be null");
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(payload);
            if (!Validator.TryValidateObject(payload, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(r => r.ErrorMessage);
                logger.LogWarning("Validation failed: {Errors}", string.Join(", ", errors));
                return new BadRequestObjectResult(new { Errors = errors });
            }

            var connectionString = EnvHelper.Get<string>("moongladestorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("Storage connection string not configured");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var queue = new QueueClient(connectionString, QueueName);

            var emailNotification = new EmailNotification
            {
                DistributionList = string.Join(';', payload.Receipts),
                MessageType = payload.Type,
                MessageBody = JsonSerializer.Serialize(payload.Payload, MoongladeJsonSerializerOptions.Default)
            };

            await InsertMessageAsync(queue, emailNotification);

            logger.LogInformation("Email notification enqueued successfully. Type={Type}, Recipients={RecipientCount}", 
                payload.Type, payload.Receipts.Length);

            return new AcceptedResult();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to serialize payload");
            return new BadRequestObjectResult("Invalid payload format");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing email enqueue request");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task InsertMessageAsync(QueueClient queue, EmailNotification emailNotification)
    {
        try
        {
            if (await queue.CreateIfNotExistsAsync() != null)
            {
                logger.LogInformation("Azure Storage Queue '{QueueName}' was created", queue.Name);
            }

            var json = JsonSerializer.Serialize(emailNotification, MoongladeJsonSerializerOptions.Default);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64Json = Convert.ToBase64String(bytes);

            await queue.SendMessageAsync(base64Json);
            
            logger.LogDebug("Message sent to queue successfully. MessageType={MessageType}", 
                emailNotification.MessageType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert message into queue '{QueueName}'", queue.Name);
            throw; // Re-throw to be handled by caller
        }
    }
}

public class EnqueueRequest
{
    [Required(ErrorMessage = "Type is required")]
    [StringLength(100, ErrorMessage = "Type must not exceed 100 characters")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one receipt is required")]
    [MinLength(1, ErrorMessage = "At least one receipt is required")]
    public string[] Receipts { get; set; } = Array.Empty<string>();

    [Required(ErrorMessage = "Payload is required")]
    public object Payload { get; set; } = new();

    public string OriginAspNetRequestId { get; set; } = Guid.Empty.ToString();
}