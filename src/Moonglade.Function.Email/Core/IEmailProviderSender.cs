using Edi.TemplateEmail;

namespace Moonglade.Function.Email.Core;

public interface IEmailProviderSender
{
    string Provider { get; }

    Task SendAsync(CommonMailMessage message);
}