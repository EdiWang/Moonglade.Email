using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Notification.Core;

namespace Moonglade.Notification.AzFunc
{
    public class EmailSendingFunction
    {
        [FunctionName("EmailSending")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("EmailSending HTTP trigger function processed a request.");

            try
            {
                var configRootDirectory = executionContext.FunctionAppDirectory;
                AppDomain.CurrentDomain.SetData(Constants.AppBaseDirectory, configRootDirectory);
                log.LogInformation($"Function App Directory: {configRootDirectory}");

                IMoongladeNotification notification = new EmailHandler(log);

                notification.EmailDisplayName = "Moonglade.Notification.AzFunc";
                notification.AdminEmail = "edi.wang@outlook.com";
                await notification.SendTestNotificationAsync();

                //string name = req.Query["name"];
                //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //dynamic data = JsonConvert.DeserializeObject(requestBody);

                return new OkObjectResult("OK");
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new ConflictObjectResult(e.Message);
            }
        }
    }
}
