using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using Moonglade.Function.Email.Core;

namespace Moonglade.Function.Email;

public class AzureCommunicationEmailClient(IOptions<EmailServiceOptions> options) : IAzureCommunicationEmailClient
{
    private readonly Lazy<EmailClient> _client = new(() => new EmailClient(options.Value.AcsConnectionString));

    public async Task<string> SendAsync(WaitUntil waitUntil, EmailMessage message)
    {
        var operation = await _client.Value.SendAsync(waitUntil, message);
        return operation.Id;
    }
}