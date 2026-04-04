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
    public string[] ToAddresses { get; set; }
}