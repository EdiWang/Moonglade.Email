using System;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Edi.TemplateEmail.NetStd;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moonglade.Notification.Models;

namespace Moonglade.Notification.Core
{
    public class EmailNotification : IMoongladeNotification
    {
        public bool IsEnabled { get; set; }

        public IEmailHelper EmailHelper { get; }

        private readonly ILogger<EmailNotification> _logger;

        public AppSettings Settings { get; set; }

        public EmailNotification(
            ILogger<EmailNotification> logger,
            IOptions<AppSettings> settings,
            IHostEnvironment env)
        {
            _logger = logger;
            Settings = settings.Value;

            IsEnabled = Settings.EnableEmailSending;
            if (env.IsDevelopment() && settings.Value.DisableEmailSendingInDevelopment)
            {
                IsEnabled = false;
            }

            if (IsEnabled)
            {
                var configSource = $@"{AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory)}\mailConfiguration.xml";
                if (!File.Exists(configSource))
                {
                    throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);
                }

                if (EmailHelper == null)
                {
                    var emailSettings = new EmailSettings(
                        Settings.SmtpServer,
                        Settings.SmtpUserName,
                        Settings.SmtpPassword,
                        Settings.SmtpServerPort)
                    {
                        EnableSsl = settings.Value.EnableSsl,
                        EmailDisplayName = settings.Value.EmailDisplayName,
                        SenderName = settings.Value.EmailDisplayName
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
        }

        public async Task<Response> SendTestNotificationAsync()
        {
            try
            {
                if (IsEnabled)
                {
                    _logger.LogInformation("Sending test mail");

                    var pipeline = new TemplatePipeline().Map(nameof(Environment.MachineName), Environment.MachineName)
                                                         .Map(nameof(EmailHelper.Settings.SmtpServer), EmailHelper.Settings.SmtpServer)
                                                         .Map(nameof(EmailHelper.Settings.SmtpServerPort), EmailHelper.Settings.SmtpServerPort)
                                                         .Map(nameof(EmailHelper.Settings.SmtpUserName), EmailHelper.Settings.SmtpUserName)
                                                         .Map(nameof(EmailHelper.Settings.EmailDisplayName), EmailHelper.Settings.EmailDisplayName)
                                                         .Map(nameof(EmailHelper.Settings.EnableSsl), EmailHelper.Settings.EnableSsl);

                    await EmailHelper.ApplyTemplate(MailMesageTypes.TestMail.ToString(), pipeline)
                                     .SendMailAsync(Settings.AdminEmail);

                    return new SuccessResponse();
                }

                return new FailedResponse((int)ResponseFailureCode.EmailSendingDisabled, "Email sending is disabled.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(SendTestNotificationAsync));
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        public async Task SendNewCommentNotificationAsync(NewCommentNotificationRequest comment, Func<string, string> funcCommentContentFormat)
        {
            if (IsEnabled)
            {
                _logger.LogInformation("Sending NewCommentNotification mail");

                var pipeline = new TemplatePipeline().Map(nameof(comment.Username), comment.Username)
                                                     .Map(nameof(comment.Email), comment.Email)
                                                     .Map(nameof(comment.IpAddress), comment.IpAddress)
                                                     .Map(nameof(comment.CreateOnUtc), comment.CreateOnUtc.ToString("MM/dd/yyyy HH:mm"))
                                                     .Map(nameof(comment.PostTitle), comment.PostTitle)
                                                     .Map(nameof(comment.CommentContent), funcCommentContentFormat(comment.CommentContent));

                await EmailHelper.ApplyTemplate(MailMesageTypes.NewCommentNotification.ToString(), pipeline)
                                 .SendMailAsync(Settings.AdminEmail);
            }
        }

        public async Task SendCommentReplyNotificationAsync(CommentReplyNotificationRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return;
            }

            if (IsEnabled)
            {
                _logger.LogInformation("Sending AdminReplyNotification mail");

                var pipeline = new TemplatePipeline().Map(nameof(model.ReplyContent), model.ReplyContent)
                                                     .Map("RouteLink", model.PostLink)
                                                     .Map("PostTitle", model.Title)
                                                     .Map(nameof(model.CommentContent), model.CommentContent);

                await EmailHelper.ApplyTemplate(MailMesageTypes.AdminReplyNotification.ToString(), pipeline)
                                 .SendMailAsync(model.Email);
            }
        }

        public async Task SendPingNotificationAsync(PingNotificationRequest receivedPingback)
        {
            if (IsEnabled)
            {
                _logger.LogInformation($"Sending BeingPinged mail for post '{receivedPingback.TargetPostTitle}'");

                var pipeline = new TemplatePipeline().Map(nameof(receivedPingback.TargetPostTitle), receivedPingback.TargetPostTitle)
                                                     .Map(nameof(receivedPingback.PingTimeUtc), receivedPingback.PingTimeUtc)
                                                     .Map(nameof(receivedPingback.Domain), receivedPingback.Domain)
                                                     .Map(nameof(receivedPingback.SourceIp), receivedPingback.SourceIp)
                                                     .Map(nameof(receivedPingback.SourceTitle), receivedPingback.SourceTitle)
                                                     .Map(nameof(receivedPingback.SourceUrl), receivedPingback.SourceUrl);

                await EmailHelper.ApplyTemplate(MailMesageTypes.BeingPinged.ToString(), pipeline)
                    .SendMailAsync(Settings.AdminEmail);
            }
        }
    }
}