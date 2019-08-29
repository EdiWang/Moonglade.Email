using System;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Edi.TemplateEmail.NetStd;
using Microsoft.Extensions.Configuration;
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
            IConfiguration configuration)
        {
            _logger = logger;

            Settings = settings.Value;

            IsEnabled = Settings.EnableEmailSending;

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
                        configuration[configuration["AzureKeyVault:SmtpPasswordKey"]],
                        Settings.SmtpServerPort)
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
        }

        public async Task<Response> SendTestNotificationAsync(NotificationRequest request)
        {
            try
            {
                if (IsEnabled)
                {
                    _logger.LogInformation("Sending test mail");

                    EmailHelper.Settings.EmailDisplayName = request.EmailDisplayName;
                    EmailHelper.Settings.SenderName = request.EmailDisplayName;

                    var pipeline = new TemplatePipeline().Map(nameof(Environment.MachineName), Environment.MachineName)
                                                         .Map(nameof(EmailHelper.Settings.SmtpServer), EmailHelper.Settings.SmtpServer)
                                                         .Map(nameof(EmailHelper.Settings.SmtpServerPort), EmailHelper.Settings.SmtpServerPort)
                                                         .Map(nameof(EmailHelper.Settings.SmtpUserName), EmailHelper.Settings.SmtpUserName)
                                                         .Map(nameof(EmailHelper.Settings.EmailDisplayName), EmailHelper.Settings.EmailDisplayName)
                                                         .Map(nameof(EmailHelper.Settings.EnableSsl), EmailHelper.Settings.EnableSsl);

                    await EmailHelper.ApplyTemplate(MailMesageTypes.TestMail.ToString(), pipeline)
                                     .SendMailAsync(request.AdminEmail);

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

        public async Task SendNewCommentNotificationAsync(NewCommentNotificationRequest request)
        {
            if (IsEnabled)
            {
                _logger.LogInformation("Sending NewCommentNotification mail");

                EmailHelper.Settings.EmailDisplayName = request.EmailDisplayName;
                EmailHelper.Settings.SenderName = request.EmailDisplayName;

                var pipeline = new TemplatePipeline().Map(nameof(request.Username), request.Username)
                                                     .Map(nameof(request.Email), request.Email)
                                                     .Map(nameof(request.IpAddress), request.IpAddress)
                                                     .Map(nameof(request.CreateOnUtc), request.CreateOnUtc.ToString("MM/dd/yyyy HH:mm"))
                                                     .Map(nameof(request.PostTitle), request.PostTitle)
                                                     .Map(nameof(request.CommentContent), request.CommentContent);

                await EmailHelper.ApplyTemplate(MailMesageTypes.NewCommentNotification.ToString(), pipeline)
                                 .SendMailAsync(request.AdminEmail);
            }
        }

        public async Task SendCommentReplyNotificationAsync(CommentReplyNotificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return;
            }

            if (IsEnabled)
            {
                _logger.LogInformation("Sending AdminReplyNotification mail");

                EmailHelper.Settings.EmailDisplayName = request.EmailDisplayName;
                EmailHelper.Settings.SenderName = request.EmailDisplayName;

                var pipeline = new TemplatePipeline().Map(nameof(request.ReplyContent), request.ReplyContent)
                                                     .Map("RouteLink", request.PostLink)
                                                     .Map("PostTitle", request.Title)
                                                     .Map(nameof(request.CommentContent), request.CommentContent);

                await EmailHelper.ApplyTemplate(MailMesageTypes.AdminReplyNotification.ToString(), pipeline)
                                 .SendMailAsync(request.Email);
            }
        }

        public async Task SendPingNotificationAsync(PingNotificationRequest request)
        {
            if (IsEnabled)
            {
                _logger.LogInformation($"Sending BeingPinged mail for post '{request.TargetPostTitle}'");

                EmailHelper.Settings.EmailDisplayName = request.EmailDisplayName;
                EmailHelper.Settings.SenderName = request.EmailDisplayName;

                var pipeline = new TemplatePipeline().Map(nameof(request.TargetPostTitle), request.TargetPostTitle)
                                                     .Map(nameof(request.PingTimeUtc), request.PingTimeUtc)
                                                     .Map(nameof(request.Domain), request.Domain)
                                                     .Map(nameof(request.SourceIp), request.SourceIp)
                                                     .Map(nameof(request.SourceTitle), request.SourceTitle)
                                                     .Map(nameof(request.SourceUrl), request.SourceUrl);

                await EmailHelper.ApplyTemplate(MailMesageTypes.BeingPinged.ToString(), pipeline)
                    .SendMailAsync(request.AdminEmail);
            }
        }
    }
}