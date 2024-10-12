using Edi.TemplateEmail;

namespace Moonglade.Function.Email;

public class AzureCommunicationSender
{
    public async Task SendAsync(CommonMailMessage message)
    {
        throw new NotImplementedException();
    }
}

public static class CommonMailMessageExtensions
{
    public static async Task SendAzureCommunicationAsync(this CommonMailMessage message)
    {
        var sender = new AzureCommunicationSender();
        await sender.SendAsync(message);
    }
}