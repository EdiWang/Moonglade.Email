using Azure;
using Azure.Communication.Email;

namespace Moonglade.Function.Email;

public interface IAzureCommunicationEmailClient
{
    Task<string> SendAsync(WaitUntil waitUntil, EmailMessage message);
}