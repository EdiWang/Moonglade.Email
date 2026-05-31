using Edi.TemplateEmail.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moonglade.Function.Email.Core;
using System.ComponentModel.DataAnnotations;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Moonglade.Function.Email;

public class TestEmail(ILogger<TestEmail> logger, MessageBuilder messageBuilder, EmailSettings smtpSettings, IEmailDispatcher dispatcher)
{
    [Function("TestEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [FromBody] TestEmailRequest payload)
    {
        logger.LogInformation("TestEmail HTTP trigger function processed a request.");

        try
        {
            if (payload == null)
            {
                logger.LogWarning("Received null test email payload");
                return new BadRequestObjectResult("Request payload cannot be null");
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(payload);
            if (!Validator.TryValidateObject(payload, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(r => r.ErrorMessage ?? "Request payload is invalid").ToArray();
                logger.LogWarning("TestEmail validation failed: {Errors}", string.Join(", ", errors));
                return new BadRequestObjectResult(new { Errors = errors });
            }

            var recipientErrors = EmailNotificationContract.ValidateRecipients(payload.ToAddresses);
            if (recipientErrors.Length > 0)
            {
                logger.LogWarning("TestEmail recipient validation failed: {Errors}", string.Join(", ", recipientErrors));
                return new BadRequestObjectResult(new { Errors = recipientErrors });
            }

            logger.LogInformation("Sending test message");

            var message = messageBuilder.BuildTestNotification(payload.ToAddresses, smtpSettings);
            await dispatcher.SendAsync(message);

            return new OkObjectResult("Test message sent");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send test email");
            return new ConflictObjectResult(e.Message);
        }
    }
}

public class TestEmailRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    public string[] ToAddresses { get; set; } = [];
}