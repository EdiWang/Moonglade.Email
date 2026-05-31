using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Moonglade.Function.Email.Core;

public class EmailServiceOptionsValidator : IValidateOptions<EmailServiceOptions>
{
    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public ValidateOptionsResult Validate(string name, EmailServiceOptions options)
    {
        var errors = new List<string>();
        var provider = options.NormalizedProvider;

        if (provider is not EmailServiceOptions.SmtpProvider and not EmailServiceOptions.AzureCommunicationProvider)
        {
            errors.Add($"MOONGLADE_EMAIL_PROVIDER '{options.Provider}' is not supported. Supported values: smtp, AzureCommunication.");
        }

        if (options.SmtpPort is < 1 or > 65535)
        {
            errors.Add("MOONGLADE_EMAIL_SMTP_PORT must be between 1 and 65535.");
        }

        switch (provider)
        {
            case EmailServiceOptions.SmtpProvider:
                ValidateSmtp(options, errors);
                break;

            case EmailServiceOptions.AzureCommunicationProvider:
                ValidateAzureCommunication(options, errors);
                break;
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateSmtp(EmailServiceOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpServer))
        {
            errors.Add("MOONGLADE_EMAIL_SMTP_SERVER is required when MOONGLADE_EMAIL_PROVIDER is smtp.");
        }

        if (string.IsNullOrWhiteSpace(options.SmtpUserName))
        {
            errors.Add("MOONGLADE_EMAIL_SMTP_USER is required when MOONGLADE_EMAIL_PROVIDER is smtp.");
        }

        if (string.IsNullOrWhiteSpace(options.SmtpPassword))
        {
            errors.Add("MOONGLADE_EMAIL_SMTP_PASS is required when MOONGLADE_EMAIL_PROVIDER is smtp.");
        }
    }

    private static void ValidateAzureCommunication(EmailServiceOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.AcsConnectionString))
        {
            errors.Add("MOONGLADE_EMAIL_ACS_CONN is required when MOONGLADE_EMAIL_PROVIDER is AzureCommunication.");
        }

        if (string.IsNullOrWhiteSpace(options.AcsSenderAddress))
        {
            errors.Add("MOONGLADE_EMAIL_ACS_ADDR is required when MOONGLADE_EMAIL_PROVIDER is AzureCommunication.");
            return;
        }

        if (!EmailAddressAttribute.IsValid(options.AcsSenderAddress))
        {
            errors.Add("MOONGLADE_EMAIL_ACS_ADDR must be a valid email address.");
        }
    }
}