using Azure.Storage.Queues.Models;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Moq;
using Moonglade.Function.Email.Core;
using Moonglade.Function.Email.Payloads;
using System.Text.Json;

namespace Moonglade.Function.Email.Tests;

public class QueueProcessorTests
{
    private readonly Mock<ILogger<QueueProcessor>> _mockLogger = new();
    private readonly Mock<IEmailDispatcher> _mockDispatcher = new();
    private readonly Mock<IEmailHelper> _mockEmailHelper = new();
    private readonly EmailSettings _smtpSettings = new()
    {
        SmtpSettings = new SmtpSettings("localhost", string.Empty, string.Empty, 25)
    };
    private readonly MessageBuilder _messageBuilder;
    private readonly QueueProcessor _sut;

    public QueueProcessorTests()
    {
        _mockEmailHelper
            .Setup(h => h.ForType(It.IsAny<string>()))
            .Returns(_mockEmailHelper.Object);
        _mockEmailHelper
            .Setup(h => h.Map(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(_mockEmailHelper.Object);
        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] });

        _messageBuilder = new MessageBuilder(_mockEmailHelper.Object);
        _sut = new QueueProcessor(_mockLogger.Object, _messageBuilder, _smtpSettings, _mockDispatcher.Object);
    }

    private static QueueMessage CreateQueueMessage(string messageText, string messageId = "msg-1")
        => QueuesModelFactory.QueueMessage(messageId, "pop-receipt", messageText, 1, null, null, null);

    [Fact]
    public async Task Run_NullDeserializedMessage_DoesNotCallDispatcher()
    {
        var queueMessage = CreateQueueMessage("null");

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_EmptyDistributionList_DoesNotCallDispatcher()
    {
        var notification = new EmailNotification
        {
            DistributionList = "",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhitespaceDistributionList_DoesNotCallDispatcher()
    {
        var notification = new EmailNotification
        {
            DistributionList = "   ",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_EmptyMessageType_DoesNotCallDispatcher()
    {
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = "",
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_TestMailType_CallsDispatcherOnce()
    {
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Once);
    }

    [Fact]
    public async Task Run_MultipleRecipients_CallsDispatcherForEach()
    {
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com;second@example.com",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Run_NewCommentType_CallsForTypeWithNewCommentNotification()
    {
        var payload = new NewCommentPayload
        {
            Username = "User",
            Email = "user@example.com",
            IpAddress = "1.1.1.1",
            PostTitle = "Post",
            CommentContent = "Nice!"
        };
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = MessageTypes.NewCommentNotification,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.NewCommentNotification), Times.Once);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Once);
    }

    [Fact]
    public async Task Run_AdminReplyType_CallsForTypeWithAdminReplyNotification()
    {
        var payload = new CommentReplyPayload
        {
            Email = "user@example.com",
            CommentContent = "Original",
            Title = "My Post",
            ReplyContentHtml = "<p>Thanks!</p>",
            PostLink = "https://example.com/posts/1"
        };
        var notification = new EmailNotification
        {
            DistributionList = "user@example.com",
            MessageType = MessageTypes.AdminReplyNotification,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.AdminReplyNotification), Times.Once);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Once);
    }

    [Fact]
    public async Task Run_BeingPingedType_CallsForTypeWithBeingPinged()
    {
        var payload = new PingPayload
        {
            TargetPostTitle = "My Post",
            Domain = "pingback.example.com",
            SourceIp = "1.2.3.4",
            SourceUrl = "https://pingback.example.com/post",
            SourceTitle = "Another Blog"
        };
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = MessageTypes.BeingPinged,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.BeingPinged), Times.Once);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Once);
    }

    [Fact]
    public async Task Run_UnknownMessageType_DoesNotCallDispatcher()
    {
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = "UnknownType",
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        var exception = await Record.ExceptionAsync(() => _sut.Run(queueMessage));

        Assert.Null(exception);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidJson_DoesNotCallDispatcher()
    {
        var queueMessage = CreateQueueMessage("{ invalid json");

        var exception = await Record.ExceptionAsync(() => _sut.Run(queueMessage));

        Assert.Null(exception);
        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidRecipient_DoesNotCallDispatcher()
    {
        var notification = new EmailNotification
        {
            DistributionList = "not-an-email",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidTypedPayload_DoesNotCallDispatcher()
    {
        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = MessageTypes.NewCommentNotification,
            MessageBody = JsonSerializer.Serialize(new { Username = "User" })
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await _sut.Run(queueMessage);

        _mockDispatcher.Verify(d => d.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_AllRecipientsFail_ThrowsAggregateException()
    {
        _mockDispatcher
            .Setup(d => d.SendAsync(It.IsAny<CommonMailMessage>()))
            .ThrowsAsync(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "fail"));

        var notification = new EmailNotification
        {
            DistributionList = "admin@example.com",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        await Assert.ThrowsAsync<AggregateException>(() => _sut.Run(queueMessage));
    }

    [Fact]
    public async Task Run_PartialRecipientsFail_DoesNotThrow()
    {
        var callCount = 0;
        _mockDispatcher
            .Setup(d => d.SendAsync(It.IsAny<CommonMailMessage>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "fail");
                return Task.CompletedTask;
            });

        var notification = new EmailNotification
        {
            DistributionList = "bad@example.com;good@example.com",
            MessageType = MessageTypes.TestMail,
            MessageBody = ""
        };
        var queueMessage = CreateQueueMessage(JsonSerializer.Serialize(notification));

        var exception = await Record.ExceptionAsync(() => _sut.Run(queueMessage));

        Assert.Null(exception);
    }
}
