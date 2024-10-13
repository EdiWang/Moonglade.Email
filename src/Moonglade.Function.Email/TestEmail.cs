using Edi.TemplateEmail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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

            var builder = new MessageBuilder(emailHelper);
            logger.LogInformation($"Sending test message");

            var message = builder.BuildTestNotification(payload.ToAddresses);

            string sender = "smtp";
            var envSender = EnvHelper.Get<string>("Sender");
            if (!string.IsNullOrWhiteSpace(envSender))
            {
                sender = envSender.ToLower();
            }

            switch (sender)
            {
                case "smtp":
                    await message.SendAsync();
                    break;

                case "azurecommunication":
                    var result = await message.SendAzureCommunicationAsync();
                    logger.LogInformation($"AzureCommunication operation ID: {result.Id}");
                    break;

                default:
                    throw new InvalidOperationException("Sender not supported");
            }

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
    [MinLength(1)]
    [MaxLength(20)]
    public string[] ToAddresses { get; set; }
}