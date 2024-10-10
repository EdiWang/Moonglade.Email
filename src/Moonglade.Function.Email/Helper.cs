using Edi.TemplateEmail;

namespace Moonglade.Function.Email;

public class Helper
{
    public static EmailHelper GetEmailHelper(string functionAppDirectory)
    {
        var configSource = Path.Join(functionAppDirectory, "mailConfiguration.xml");
        if (!File.Exists(configSource))
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

        var settings = new EmailSettings(
            Environment.GetEnvironmentVariable("SmtpServer"),
            Environment.GetEnvironmentVariable("SmtpUserName"),
            Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
            int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "25"));

        var emailHelper = new EmailHelper(configSource, settings);

        var dName = Environment.GetEnvironmentVariable("SenderDisplayName");
        if (!string.IsNullOrWhiteSpace(dName)) settings.EmailDisplayName = dName;

        if (bool.Parse(Environment.GetEnvironmentVariable("EnableTls") ?? "true")) ;
        settings.EnableTls = true;

        return emailHelper;
    }
}