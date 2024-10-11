using Edi.TemplateEmail;

namespace Moonglade.Function.Email.Core;

public class Helper
{
    public static EmailHelper GetEmailHelper(string functionAppDirectory)
    {
        var configSource = Path.Join(functionAppDirectory, "mailConfiguration.xml");
        if (!File.Exists(configSource))
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

        var smtpSettings = new SmtpSettings(Environment.GetEnvironmentVariable("SmtpServer"),
            Environment.GetEnvironmentVariable("SmtpUserName"),
            Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
            int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "25"));

        if (bool.Parse(Environment.GetEnvironmentVariable("EnableTls") ?? "true"))
        {
            smtpSettings.EnableTls = true;
        }

        var settings = new EmailSettings
        {
            SmtpSettings = smtpSettings
        };

        var dName = Environment.GetEnvironmentVariable("SenderDisplayName");
        if (!string.IsNullOrWhiteSpace(dName)) settings.EmailDisplayName = dName;

        var emailHelper = new EmailHelper(configSource, settings);
        return emailHelper;
    }
}