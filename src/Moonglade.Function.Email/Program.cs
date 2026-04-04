using Azure.Storage.Queues;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moonglade.Function.Email;
using Moonglade.Function.Email.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEmailHelper>(_ => Helper.GetEmailHelper(Environment.CurrentDirectory));
        services.AddSingleton(_ => Helper.GetSmtpSettings());
        services.AddSingleton<MessageBuilder>();
        services.AddSingleton(_ =>
        {
            var connectionString = EnvHelper.Get<string>("MOONGLADE_EMAIL_STORAGE");
            return new QueueClient(connectionString, Enqueue.QueueName);
        });
    })
    .Build();

host.Run();
