using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Notification.Core
{
    public class AppSettings
    {
        public bool DisableEmailSendingInDevelopment { get; set; }
        public bool EnableEmailSending { get; set; }
        public bool EnableSsl { get; set; }
        public int SmtpServerPort { get; set; }
        public string AdminEmail { get; set; }
        public string EmailDisplayName { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpServer { get; set; }
        public string SmtpUserName { get; set; }
        public string BannedMailDomain { get; set; }
    }
}
