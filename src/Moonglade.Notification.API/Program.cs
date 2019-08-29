using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace Moonglade.Notification.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true)
                              .ConfigureAppConfiguration((ctx, builder) =>
                              {
                                  var builtConfig = builder.Build();
                                  var azureServiceTokenProvider = new AzureServiceTokenProvider();
                                  var keyVaultClient = new KeyVaultClient(
                                      new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                                  builder.AddAzureKeyVault(
                                      $"https://{builtConfig["AzureKeyVault:Name"]}.vault.azure.net/", 
                                      keyVaultClient, 
                                      new DefaultKeyVaultSecretManager());
                              })
                              .ConfigureKestrel(c => c.AddServerHeader = false)
                              .UseIISIntegration()
                              .UseStartup<Startup>()
                              .ConfigureLogging(logging =>
                              {
                                  logging.AddAzureWebAppDiagnostics();
                              });
                });
    }
}
