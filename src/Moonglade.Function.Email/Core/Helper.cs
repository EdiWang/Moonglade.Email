using Edi.TemplateEmail;

namespace Moonglade.Function.Email.Core;

public class Helper
{
    public static EmailHelper GetEmailHelper(string functionAppDirectory)
    {
        var configSource = Path.Join(functionAppDirectory, "mailConfiguration.xml");
        if (!File.Exists(configSource))
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

        var smtpSettings = new SmtpSettings(EnvHelper.Get<string>("MOONGLADE_EMAIL_SMTP_SERVER"),
            EnvHelper.Get<string>("MOONGLADE_EMAIL_SMTP_USER"),
            EnvHelper.Get<string>("MOONGLADE_EMAIL_SMTP_PASS", target: EnvironmentVariableTarget.Process),
            EnvHelper.Get("MOONGLADE_EMAIL_SMTP_PORT", 25));

        if (EnvHelper.Get<bool>("MOONGLADE_EMAIL_SSL"))
        {
            smtpSettings.EnableTls = true;
        }

        var settings = new EmailSettings
        {
            SmtpSettings = smtpSettings
        };

        var dName = EnvHelper.Get<string>("MOONGLADE_EMAIL_SENDER_NAME");
        if (!string.IsNullOrWhiteSpace(dName)) settings.EmailDisplayName = dName;

        var emailHelper = new EmailHelper(configSource, settings);
        return emailHelper;
    }
}