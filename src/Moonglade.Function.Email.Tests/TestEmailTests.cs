using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moonglade.Function.Email.Core;

namespace Moonglade.Function.Email.Tests;

public class TestEmailTests
{
    private readonly Mock<ILogger<TestEmail>> _mockLogger = new();
    private readonly Mock<IEmailDispatcher> _mockDispatcher = new();
    private readonly Mock<IEmailHelper> _mockEmailHelper = new();
    private readonly EmailSettings _smtpSettings = new()
    {
        SmtpSettings = new SmtpSettings("localhost", string.Empty, string.Empty, 25)
    };

    public TestEmailTests()
    {
        _mockEmailHelper
            .Setup(h => h.ForType(It.IsAny<string>()))
            .Returns(_mockEmailHelper.Object);
        _mockEmailHelper
            .Setup(h => h.Map(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(_mockEmailHelper.Object);
        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()));
    }

    private TestEmail CreateSut()
    {
        var messageBuilder = new MessageBuilder(_mockEmailHelper.Object);
        return new TestEmail(_mockLogger.Object, messageBuilder, _smtpSettings, _mockDispatcher.Object);
    }

    [Fact]
    public async Task Run_NullPayload_ReturnsBadRequest()
    {
        var sut = CreateSut();

        var result = await sut.Run(new DefaultHttpContext().Request, null);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidRecipient_ReturnsBadRequest()
    {
        var sut = CreateSut();
        var request = new TestEmailRequest { ToAddresses = ["not-an-email"] };

        var result = await sut.Run(new DefaultHttpContext().Request, request);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_ValidPayload_SendsTestEmail()
    {
        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage { Subject = "Test", Receipts = ["admin@example.com"] });

        var sut = CreateSut();
        var request = new TestEmailRequest { ToAddresses = ["admin@example.com"] };

        var result = await sut.Run(new DefaultHttpContext().Request, request);

        Assert.IsType<OkObjectResult>(result);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Once);
    }
}