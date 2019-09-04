using System;
using System.IO;
using System.Threading.Tasks;
using Edi.TemplateEmail.NetStd;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moonglade.Notification.Models;

namespace Moonglade.Notification.Core
{
    public class EmailHandler : IMoongladeNotification
    {
        public IEmailHelper EmailHelper { get; }

        public string EmailDisplayName { get; set; }

        public string AdminEmail { get; set; }

        private readonly ILogger<EmailHandler> _logger;

        public EmailHandler(
            ILogger<EmailHandler> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration)
        {
            _logger = logger;

            var configSource = $@"{AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory)}\mailConfiguration.xml";
            if (!File.Exists(configSource))
            {
                throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);
            }

            if (EmailHelper == null)
            {
                var emailSettings = new EmailSettings(
                    settings.Value.SmtpServer,
                    settings.Value.SmtpUserName,
                    configuration[configuration["AzureKeyVault:SmtpPasswordKey"]],
                    settings.Value.SmtpServerPort)
                {
                    EnableSsl = settings.Value.EnableSsl
                };

                EmailHelper = new EmailHelper(configSource, emailSettings);
                EmailHelper.EmailSent += (sender, eventArgs) =>
                {
                    if (sender is MimeMessage msg)
                    {
                        _logger.LogInformation($"Email {msg.Subject} is sent, Success: {eventArgs.IsSuccess}");
                    }
                };
            }
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
                                                 .Map(nameof(EmailHelper.Settings.EnableSsl), EmailHelper.Settings.EnableSsl);

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

            var pipeline = new TemplatePipeline().Map(nameof(request.ReplyContent), request.ReplyContent)
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

            EmailHelper.Settings.EmailDisplayName = EmailDisplayName;
            EmailHelper.Settings.SenderName = EmailDisplayName;
        }
    }
}