using Microsoft.Extensions.Options;
using Moonglade.Function.Email.Core;

namespace Moonglade.Function.Email.Tests;

public class EmailQueueOptionsValidatorTests
{
    private readonly EmailQueueOptionsValidator _sut = new();

    [Fact]
    public void Validate_StorageConnectionStringPresent_Succeeds()
    {
        var options = new EmailQueueOptions { StorageConnectionString = "UseDevelopmentStorage=true" };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_StorageConnectionStringMissing_Fails()
    {
        var options = new EmailQueueOptions();

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("MOONGLADE_EMAIL_STORAGE", result.FailureMessage);
    }
}