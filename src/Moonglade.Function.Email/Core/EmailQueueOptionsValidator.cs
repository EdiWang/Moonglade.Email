using Microsoft.Extensions.Options;

namespace Moonglade.Function.Email.Core;

public class EmailQueueOptionsValidator : IValidateOptions<EmailQueueOptions>
{
    public ValidateOptionsResult Validate(string name, EmailQueueOptions options)
    {
        return string.IsNullOrWhiteSpace(options.StorageConnectionString)
            ? ValidateOptionsResult.Fail("MOONGLADE_EMAIL_STORAGE is required and must contain an Azure Storage connection string.")
            : ValidateOptionsResult.Success;
    }
}