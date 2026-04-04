using Azure.Storage.Queues;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moonglade.Function.Email;
using Moonglade.Function.Email.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.Configure<EmailServiceOptions>(opts =>
        {
            opts.SmtpServer = config["MOONGLADE_EMAIL_SMTP_SERVER"] ?? string.Empty;
            opts.SmtpUserName = config["MOONGLADE_EMAIL_SMTP_USER"] ?? string.Empty;
            opts.SmtpPassword = config["MOONGLADE_EMAIL_SMTP_PASS"] ?? string.Empty;
            opts.SmtpPort = int.TryParse(config["MOONGLADE_EMAIL_SMTP_PORT"], out var port) ? port : 25;
            opts.EnableSsl = bool.TryParse(config["MOONGLADE_EMAIL_SSL"], out var ssl) && ssl;
            opts.SenderDisplayName = config["MOONGLADE_EMAIL_SENDER_NAME"] ?? string.Empty;
        });

        services.AddSingleton<IEmailHelper>(_ =>
        {
            var configSource = Path.Join(Environment.CurrentDirectory, "mailConfiguration.xml");
            if (!File.Exists(configSource))
                throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);
            return new EmailHelper(configSource);
        });

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailServiceOptions>>().Value;
            var smtpSettings = new SmtpSettings(opts.SmtpServer, opts.SmtpUserName, opts.SmtpPassword, opts.SmtpPort);
            smtpSettings.EnableTls = opts.EnableSsl;
            var settings = new EmailSettings { SmtpSettings = smtpSettings };
            if (!string.IsNullOrWhiteSpace(opts.SenderDisplayName))
                settings.EmailDisplayName = opts.SenderDisplayName;
            return settings;
        });

        services.AddSingleton<MessageBuilder>();

        services.AddSingleton(_ =>
        {
            var connectionString = config["MOONGLADE_EMAIL_STORAGE"] ?? string.Empty;
            return new QueueClient(connectionString, Enqueue.QueueName);
        });
    })
    .Build();

host.Run();
