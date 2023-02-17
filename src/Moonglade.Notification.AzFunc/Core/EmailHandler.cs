using Edi.TemplateEmail;
using Moonglade.Notification.AzFunc.Payloads;

namespace Moonglade.Notification.AzFunc.Core;

public class EmailHandler
{
    private readonly IEmailHelper _emailHelper;

    public EmailHandler(IEmailHelper emailHelper, string emailDisplayName)
    {
        _emailHelper = emailHelper;

        emailHelper.WithDisplayName(emailDisplayName);
        emailHelper.WithSenderName(emailDisplayName);
    }

    public async Task SendTestNotificationAsync(string[] toAddresses)
    {
        await _emailHelper.ForType("TestMail")
                          .Map(nameof(Environment.MachineName), Environment.MachineName)
                          .Map(nameof(EmailHelper.Settings.SmtpServer), _emailHelper.Settings.SmtpServer)
                          .Map(nameof(EmailHelper.Settings.SmtpServerPort), _emailHelper.Settings.SmtpServerPort)
                          .Map(nameof(EmailHelper.Settings.SmtpUserName), _emailHelper.Settings.SmtpUserName)
                          .Map(nameof(EmailHelper.Settings.EmailDisplayName), _emailHelper.Settings.EmailDisplayName)
                          .Map(nameof(EmailHelper.Settings.EnableTls), _emailHelper.Settings.EnableTls)
                          .SendAsync(toAddresses);
    }

    public async Task SendNewCommentNotificationAsync(string[] toAddresses, NewCommentPayload request)
    {
        await _emailHelper.ForType("NewCommentNotification")
                          .Map(nameof(request.Username), request.Username)
                          .Map(nameof(request.Email), request.Email)
                          .Map(nameof(request.IpAddress), request.IpAddress)
                          .Map(nameof(request.PostTitle), request.PostTitle)
                          .Map(nameof(request.CommentContent), request.CommentContent)
                          .SendAsync(toAddresses);
    }

    public async Task SendCommentReplyNotificationAsync(string[] toAddress, CommentReplyPayload request)
    {
        await _emailHelper.ForType("AdminReplyNotification")
                          .Map(nameof(request.ReplyContentHtml), request.ReplyContentHtml)
                          .Map("RouteLink", request.PostLink)
                          .Map("PostTitle", request.Title)
                          .Map(nameof(request.CommentContent), request.CommentContent)
                          .SendAsync(toAddress);
    }

    public async Task SendPingNotificationAsync(string[] toAddresses, PingPayload request)
    {
        await _emailHelper.ForType("BeingPinged")
                          .Map(nameof(request.TargetPostTitle), request.TargetPostTitle)
                          .Map(nameof(request.Domain), request.Domain)
                          .Map(nameof(request.SourceIp), request.SourceIp)
                          .Map(nameof(request.SourceTitle), request.SourceTitle)
                          .Map(nameof(request.SourceUrl), request.SourceUrl)
                          .SendAsync(toAddresses);
    }
}