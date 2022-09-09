using Edi.TemplateEmail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moonglade.Notification.AzFunc.Core;
using System.ComponentModel.DataAnnotations;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Moonglade.Notification.AzFunc;

public class TestEmail
{
    [FunctionName("TestEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] TestEmailRequest request,
        ILogger log, ExecutionContext executionContext)
    {
        log.LogInformation("EmailSending HTTP trigger function processed a request.");

        try
        {
            var configSource = Path.Join(executionContext.FunctionAppDirectory, "mailConfiguration.xml");
            if (!File.Exists(configSource)) throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

            var emailHelper = new EmailHelper(
                configSource,
                Environment.GetEnvironmentVariable("SmtpServer"),
                Environment.GetEnvironmentVariable("SmtpUserName"),
                Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
                int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "587"));

            if (bool.Parse(Environment.GetEnvironmentVariable("EnableTls") ?? "true")) emailHelper.WithTls();

            emailHelper.EmailSent += (sender, eventArgs) =>
            {
                if (sender is MimeMessage msg)
                {
                    log.LogInformation($"Email '{msg.Subject}' is sent. Success: {eventArgs.IsSuccess}");
                }
            };

            emailHelper.EmailFailed += (sender, args) =>
            {
                if (sender is MimeMessage msg)
                {
                    log.LogError($"Email '{msg.Subject}' failed: {args.ServerResponse}");
                }
            };

            var notification = new EmailHandler(emailHelper, request.EmailDisplayName);
            log.LogInformation($"Sending test message");

            await notification.SendTestNotificationAsync(request.ToAddresses);

            return new OkObjectResult($"Test message sent");

        }
        catch (Exception e)
        {
            log.LogError(e, e.Message);
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