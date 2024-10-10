using Edi.TemplateEmail;
using Moonglade.Function.Email.Payloads;

namespace Moonglade.Function.Email.Core;

public class MessageBuilder(IEmailHelper emailHelper)
{
    public MimeMessageWithSettings BuildTestNotification(string[] toAddresses)
    {
        var message = emailHelper.ForType("TestMail")
                          .Map(nameof(Environment.MachineName), Environment.MachineName)
                          .Map(nameof(EmailHelper.Settings.SmtpServer), emailHelper.Settings.SmtpServer)
                          .Map(nameof(EmailHelper.Settings.SmtpServerPort), emailHelper.Settings.SmtpServerPort)
                          .Map(nameof(EmailHelper.Settings.SmtpUserName), emailHelper.Settings.SmtpUserName)
                          .Map(nameof(EmailHelper.Settings.EmailDisplayName), emailHelper.Settings.EmailDisplayName)
                          .Map(nameof(EmailHelper.Settings.EnableTls), emailHelper.Settings.EnableTls)
                          .BuildMessage(toAddresses);
        return message;
    }

    public MimeMessageWithSettings BuildNewCommentNotification(string[] toAddresses, NewCommentPayload request)
    {
        var message = emailHelper.ForType("NewCommentNotification")
                          .Map(nameof(request.Username), request.Username)
                          .Map(nameof(request.Email), request.Email)
                          .Map(nameof(request.IpAddress), request.IpAddress)
                          .Map(nameof(request.PostTitle), request.PostTitle)
                          .Map(nameof(request.CommentContent), request.CommentContent)
                          .BuildMessage(toAddresses);
        return message;
    }

    public MimeMessageWithSettings BuildCommentReplyNotification(string[] toAddress, CommentReplyPayload request)
    {
        var message = emailHelper.ForType("AdminReplyNotification")
                          .Map(nameof(request.ReplyContentHtml), request.ReplyContentHtml)
                          .Map("RouteLink", request.PostLink)
                          .Map("PostTitle", request.Title)
                          .Map(nameof(request.CommentContent), request.CommentContent)
                          .BuildMessage(toAddress);
        return message;
    }

    public MimeMessageWithSettings BuildPingNotification(string[] toAddresses, PingPayload request)
    {
        var message = emailHelper.ForType("BeingPinged")
                          .Map(nameof(request.TargetPostTitle), request.TargetPostTitle)
                          .Map(nameof(request.Domain), request.Domain)
                          .Map(nameof(request.SourceIp), request.SourceIp)
                          .Map(nameof(request.SourceTitle), request.SourceTitle)
                          .Map(nameof(request.SourceUrl), request.SourceUrl)
                          .BuildMessage(toAddresses);
        return message;
    }
}