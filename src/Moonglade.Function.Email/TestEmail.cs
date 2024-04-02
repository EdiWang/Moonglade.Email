using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moonglade.Function.Email.Core;
using System.ComponentModel.DataAnnotations;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Moonglade.Function.Email;

public class TestEmail(ILogger<TestEmail> logger)
{
    [Function("TestEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [FromBody] TestEmailRequest payload,
        Microsoft.Azure.WebJobs.ExecutionContext executionContext)
    {
        logger.LogInformation("EmailSending HTTP trigger function processed a request.");

        try
        {
            var runningDirectory = Environment.CurrentDirectory;
            var emailHelper = Helper.GetEmailHelper(runningDirectory);

            emailHelper.EmailSent += (sender, eventArgs) =>
            {
                if (sender is MimeMessage msg)
                {
                    logger.LogInformation($"Email '{msg.Subject}' is sent. Success: {eventArgs.IsSuccess}");
                }
            };

            emailHelper.EmailFailed += (sender, args) =>
            {
                if (sender is MimeMessage msg)
                {
                    logger.LogError($"Email '{msg.Subject}' failed: {args.ServerResponse}");
                }
            };

            var notification = new EmailHandler(emailHelper, payload.EmailDisplayName);
            logger.LogInformation($"Sending test message");

            await notification.SendTestNotificationAsync(payload.ToAddresses);

            return new OkObjectResult($"Test message sent");

        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ConflictObjectResult(e.Message);
        }
    }
}

public class TestEmailRequest
{
    [Required]
    public string EmailDisplayName { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    public string[] ToAddresses { get; set; }
}