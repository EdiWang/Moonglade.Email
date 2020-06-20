using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Notification.Core;

[assembly: FunctionsStartup(typeof(Moonglade.Notification.AzFunc.Startup))]
namespace Moonglade.Notification.AzFunc
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddScoped<IMoongladeNotification, EmailHandler>();
        }
    }
}
