using Edi.TemplateEmail;

namespace Moonglade.Function.Email.Core;

public interface IEmailDispatcher
{
    Task SendAsync(CommonMailMessage message);
}
