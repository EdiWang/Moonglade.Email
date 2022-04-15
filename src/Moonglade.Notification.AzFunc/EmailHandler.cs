using Edi.TemplateEmail;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Moonglade.Notification.AzFunc;

public class EmailHandler
{
    public IEmailHelper EmailHelper { get; }

    public string EmailDisplayName { get; private init; }

    public string AdminEmail { get; private init; }

    private readonly ILogger _logger;

    public EmailHandler(ILogger logger, string emailDisplayName, string adminEmail)
    {
        _logger = logger;
        EmailDisplayName = emailDisplayName;
        AdminEmail = adminEmail;

        var configSource = Path.Join($"{AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory)}", "mailConfiguration.xml");
        if (!File.Exists(configSource))
        {
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);
        }

        EmailHelper = new EmailHelper(configSource,
            Environment.GetEnvironmentVariable("SmtpServer"),
            Environment.GetEnvironmentVariable("SmtpUserName"),
            Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
            int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "587"));

        if (bool.Parse(Environment.GetEnvironmentVariable("EnableSsl") ?? "true")) EmailHelper.WithTls();

        EmailHelper.EmailSent += (sender, eventArgs) =>
        {
            if (sender is MimeMessage msg)
            {
                _logger.LogInformation($"Email {msg.Subject} is sent, Success: {eventArgs.IsSuccess}");
            }
        };
    }

    public async Task SendTestNotificationAsync()
    {
        _logger.LogInformation("Sending test mail");

        SetEmailInfo();

        var pipeline = new TemplatePipeline().Map(nameof(Environment.MachineName), Environment.MachineName)
            .Map(nameof(EmailHelper.Settings.SmtpServer), EmailHelper.Settings.SmtpServer)
            .Map(nameof(EmailHelper.Settings.SmtpServerPort), EmailHelper.Settings.SmtpServerPort)
            .Map(nameof(EmailHelper.Settings.SmtpUserName), EmailHelper.Settings.SmtpUserName)
            .Map(nameof(EmailHelper.Settings.EmailDisplayName), EmailHelper.Settings.EmailDisplayName)
            .Map(nameof(EmailHelper.Settings.EnableTls), EmailHelper.Settings.EnableTls);

        await EmailHelper.ApplyTemplate(MailMesageTypes.TestMail.ToString(), pipeline)
            .SendMailAsync(AdminEmail);
    }

    public async Task SendNewCommentNotificationAsync(NewCommentPayload request)
    {
        _logger.LogInformation("Sending NewCommentNotification mail");

        SetEmailInfo();

        var pipeline = new TemplatePipeline().Map(nameof(request.Username), request.Username)
            .Map(nameof(request.Email), request.Email)
            .Map(nameof(request.IpAddress), request.IpAddress)
            .Map(nameof(request.CreateOnUtc), request.CreateOnUtc.ToString("MM/dd/yyyy HH:mm"))
            .Map(nameof(request.PostTitle), request.PostTitle)
            .Map(nameof(request.CommentContent), request.CommentContent);

        await EmailHelper.ApplyTemplate(MailMesageTypes.NewCommentNotification.ToString(), pipeline)
            .SendMailAsync(AdminEmail);
    }

    public async Task SendCommentReplyNotificationAsync(CommentReplyPayload request)
    {
        _logger.LogInformation("Sending AdminReplyNotification mail");

        SetEmailInfo();

        var pipeline = new TemplatePipeline().Map(nameof(request.ReplyContentHtml), request.ReplyContentHtml)
            .Map("RouteLink", request.PostLink)
            .Map("PostTitle", request.Title)
            .Map(nameof(request.CommentContent), request.CommentContent);

        await EmailHelper.ApplyTemplate(MailMesageTypes.AdminReplyNotification.ToString(), pipeline)
            .SendMailAsync(request.Email);
    }

    public async Task SendPingNotificationAsync(PingPayload request)
    {
        _logger.LogInformation($"Sending BeingPinged mail for post '{request.TargetPostTitle}'");

        SetEmailInfo();

        var pipeline = new TemplatePipeline().Map(nameof(request.TargetPostTitle), request.TargetPostTitle)
            .Map(nameof(request.PingTimeUtc), request.PingTimeUtc)
            .Map(nameof(request.Domain), request.Domain)
            .Map(nameof(request.SourceIp), request.SourceIp)
            .Map(nameof(request.SourceTitle), request.SourceTitle)
            .Map(nameof(request.SourceUrl), request.SourceUrl);

        await EmailHelper.ApplyTemplate(MailMesageTypes.BeingPinged.ToString(), pipeline)
            .SendMailAsync(AdminEmail);
    }

    private void SetEmailInfo()
    {
        if (string.IsNullOrWhiteSpace(EmailDisplayName))
        {
            throw new ArgumentNullException(nameof(EmailDisplayName));
        }

        if (string.IsNullOrWhiteSpace(AdminEmail))
        {
            throw new ArgumentNullException(nameof(AdminEmail));
        }

        EmailHelper.WithDisplayName(EmailDisplayName);
        EmailHelper.WithSenderName(EmailDisplayName);
    }
}