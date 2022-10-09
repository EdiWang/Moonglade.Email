using Edi.TemplateEmail;

namespace Moonglade.Notification.AzFunc;

public class Helper
{
    public static EmailHelper GetEmailHelper(string functionAppDirectory)
    {
        var configSource = Path.Join(functionAppDirectory, "mailConfiguration.xml");
        if (!File.Exists(configSource))
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

        var emailHelper = new EmailHelper(
            configSource,
            Environment.GetEnvironmentVariable("SmtpServer"),
            Environment.GetEnvironmentVariable("SmtpUserName"),
            Environment.GetEnvironmentVariable("EmailAccountPassword", EnvironmentVariableTarget.Process),
            int.Parse(Environment.GetEnvironmentVariable("SmtpServerPort") ?? "587"));

        if (bool.Parse(Environment.GetEnvironmentVariable("EnableTls") ?? "true")) emailHelper.WithTls();

        return emailHelper;
    }
}