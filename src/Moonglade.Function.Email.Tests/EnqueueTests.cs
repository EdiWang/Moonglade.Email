using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moonglade.Function.Email.Core;
using Moonglade.Function.Email.Payloads;

namespace Moonglade.Function.Email.Tests;

public class EnqueueTests
{
    private readonly Mock<ILogger<Enqueue>> _mockLogger = new();
    private readonly Mock<IEmailNotificationQueue> _mockQueue = new();

    private Enqueue CreateSut() => new(_mockLogger.Object, _mockQueue.Object);

    [Fact]
    public async Task Run_NullPayload_ReturnsBadRequest()
    {
        var sut = CreateSut();

        var result = await sut.Run(new DefaultHttpContext().Request, null);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockQueue.Verify(q => q.SendAsync(It.IsAny<EmailNotification>()), Times.Never);
    }

    [Fact]
    public async Task Run_UnsupportedType_ReturnsBadRequest()
    {
        var sut = CreateSut();
        var request = new EnqueueRequest
        {
            Type = "UnknownType",
            Recipients = ["admin@example.com"],
            Payload = new { }
        };

        var result = await sut.Run(new DefaultHttpContext().Request, request);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockQueue.Verify(q => q.SendAsync(It.IsAny<EmailNotification>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidRecipient_ReturnsBadRequest()
    {
        var sut = CreateSut();
        var request = new EnqueueRequest
        {
            Type = MessageTypes.TestMail,
            Recipients = ["not-an-email"],
            Payload = new { }
        };

        var result = await sut.Run(new DefaultHttpContext().Request, request);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockQueue.Verify(q => q.SendAsync(It.IsAny<EmailNotification>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidTypedPayload_ReturnsBadRequest()
    {
        var sut = CreateSut();
        var request = new EnqueueRequest
        {
            Type = MessageTypes.NewCommentNotification,
            Recipients = ["admin@example.com"],
            Payload = new { Username = "User" }
        };

        var result = await sut.Run(new DefaultHttpContext().Request, request);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockQueue.Verify(q => q.SendAsync(It.IsAny<EmailNotification>()), Times.Never);
    }

    [Fact]
    public async Task Run_ValidRequest_QueuesEmailNotification()
    {
        var sut = CreateSut();
        EmailNotification capturedNotification = null;
        _mockQueue
            .Setup(q => q.SendAsync(It.IsAny<EmailNotification>()))
            .Callback<EmailNotification>(notification => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var request = new EnqueueRequest
        {
            Type = MessageTypes.NewCommentNotification,
            Recipients = ["admin@example.com"],
            Payload = new NewCommentPayload
            {
                Username = "User",
                Email = "user@example.com",
                IpAddress = "127.0.0.1",
                PostTitle = "Post",
                CommentContent = "Nice post"
            }
        };

        var result = await sut.Run(new DefaultHttpContext().Request, request);

        Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(capturedNotification);
        Assert.Equal(MessageTypes.NewCommentNotification, capturedNotification.MessageType);
        Assert.Equal("admin@example.com", capturedNotification.DistributionList);
        _mockQueue.Verify(q => q.SendAsync(It.IsAny<EmailNotification>()), Times.Once);
    }
}