using Edi.TemplateEmail;

namespace Moonglade.Notification.AzFunc;

public class EmailHandler
{
    private readonly string _adminEmail;
    private readonly IEmailHelper _emailHelper;

    public EmailHandler(IEmailHelper emailHelper, string emailDisplayName, string adminEmail)
    {
        _emailHelper = emailHelper;

        emailHelper.WithDisplayName(emailDisplayName);
        emailHelper.WithSenderName(emailDisplayName);

        _adminEmail = adminEmail;
    }

    public async Task SendTestNotificationAsync()
    {
        var pipeline = new TemplatePipeline().Map(nameof(Environment.MachineName), Environment.MachineName)
            .Map(nameof(EmailHelper.Settings.SmtpServer), _emailHelper.Settings.SmtpServer)
            .Map(nameof(EmailHelper.Settings.SmtpServerPort), _emailHelper.Settings.SmtpServerPort)
            .Map(nameof(EmailHelper.Settings.SmtpUserName), _emailHelper.Settings.SmtpUserName)
            .Map(nameof(EmailHelper.Settings.EmailDisplayName), _emailHelper.Settings.EmailDisplayName)
            .Map(nameof(EmailHelper.Settings.EnableTls), _emailHelper.Settings.EnableTls);

        await _emailHelper.ApplyTemplate(MailMesageTypes.TestMail.ToString(), pipeline)
            .SendMailAsync(_adminEmail);
    }

    public async Task SendNewCommentNotificationAsync(NewCommentPayload request)
    {
        var pipeline = new TemplatePipeline().Map(nameof(request.Username), request.Username)
            .Map(nameof(request.Email), request.Email)
            .Map(nameof(request.IpAddress), request.IpAddress)
            .Map(nameof(request.CreateOnUtc), request.CreateOnUtc.ToString("MM/dd/yyyy HH:mm"))
            .Map(nameof(request.PostTitle), request.PostTitle)
            .Map(nameof(request.CommentContent), request.CommentContent);

        await _emailHelper.ApplyTemplate(MailMesageTypes.NewCommentNotification.ToString(), pipeline)
            .SendMailAsync(_adminEmail);
    }

    public async Task SendCommentReplyNotificationAsync(CommentReplyPayload request)
    {
        var pipeline = new TemplatePipeline().Map(nameof(request.ReplyContentHtml), request.ReplyContentHtml)
            .Map("RouteLink", request.PostLink)
            .Map("PostTitle", request.Title)
            .Map(nameof(request.CommentContent), request.CommentContent);

        await _emailHelper.ApplyTemplate(MailMesageTypes.AdminReplyNotification.ToString(), pipeline)
            .SendMailAsync(request.Email);
    }

    public async Task SendPingNotificationAsync(PingPayload request)
    {
        var pipeline = new TemplatePipeline().Map(nameof(request.TargetPostTitle), request.TargetPostTitle)
            .Map(nameof(request.PingTimeUtc), request.PingTimeUtc)
            .Map(nameof(request.Domain), request.Domain)
            .Map(nameof(request.SourceIp), request.SourceIp)
            .Map(nameof(request.SourceTitle), request.SourceTitle)
            .Map(nameof(request.SourceUrl), request.SourceUrl);

        await _emailHelper.ApplyTemplate(MailMesageTypes.BeingPinged.ToString(), pipeline)
            .SendMailAsync(_adminEmail);
    }
}