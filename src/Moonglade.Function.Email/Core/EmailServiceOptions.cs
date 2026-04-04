namespace Moonglade.Function.Email.Core;

public class EmailServiceOptions
{
    public string SmtpServer { get; set; } = string.Empty;
    public string SmtpUserName { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Email sending provider. Supported values: "smtp" (default), "azurecommunication".
    /// Maps to environment variable: MOONGLADE_EMAIL_PROVIDER
    /// </summary>
    public string Provider { get; set; } = "smtp";

    /// <summary>
    /// Azure Communication Services connection string.
    /// Maps to environment variable: MOONGLADE_EMAIL_ACS_CONN
    /// </summary>
    public string AcsConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Azure Communication Services sender email address.
    /// Maps to environment variable: MOONGLADE_EMAIL_ACS_ADDR
    /// </summary>
    public string AcsSenderAddress { get; set; } = string.Empty;
}
