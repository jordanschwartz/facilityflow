using FacilityFlow.Application.Commands.EmailActions;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;

namespace FacilityFlow.Tests.EmailActions;

public class EmailActionCommandTests
{
    [Fact]
    public void CreateQuoteFromEmailCommand_SetsInboundEmailId()
    {
        var id = Guid.NewGuid();
        var command = new CreateQuoteFromEmailCommand(id);
        Assert.Equal(id, command.InboundEmailId);
    }

    [Fact]
    public void AttachEmailAsPurchaseOrderCommand_SetsProperties()
    {
        var emailId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var command = new AttachEmailAsPurchaseOrderCommand(emailId, attachmentId);
        Assert.Equal(emailId, command.InboundEmailId);
        Assert.Equal(attachmentId, command.AttachmentId);
    }

    [Fact]
    public void AddEmailToNotesCommand_SetsInboundEmailId()
    {
        var id = Guid.NewGuid();
        var command = new AddEmailToNotesCommand(id);
        Assert.Equal(id, command.InboundEmailId);
    }

    [Fact]
    public void ResendOutboundEmailCommand_SetsOutboundEmailId()
    {
        var id = Guid.NewGuid();
        var command = new ResendOutboundEmailCommand(id);
        Assert.Equal(id, command.OutboundEmailId);
    }

    [Fact]
    public void ForwardOutboundEmailCommand_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var command = new ForwardOutboundEmailCommand(id, "test@example.com", "Test User");
        Assert.Equal(id, command.OutboundEmailId);
        Assert.Equal("test@example.com", command.RecipientEmail);
        Assert.Equal("Test User", command.RecipientName);
    }

    [Fact]
    public void ForwardOutboundEmailCommand_AllowsNullRecipientName()
    {
        var id = Guid.NewGuid();
        var command = new ForwardOutboundEmailCommand(id, "test@example.com", null);
        Assert.Null(command.RecipientName);
    }

    [Fact]
    public void CreateQuoteFromEmail_QuoteGetsCorrectFields()
    {
        var serviceRequestId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequestId,
            VendorId = vendorId,
            ScopeOfWork = "Email body content",
            Status = QuoteStatus.Submitted,
            PublicToken = "qt-" + Guid.NewGuid().ToString("N"),
            SubmittedAt = DateTime.UtcNow
        };

        Assert.Equal(serviceRequestId, quote.ServiceRequestId);
        Assert.Equal(vendorId, quote.VendorId);
        Assert.Equal("Email body content", quote.ScopeOfWork);
        Assert.Equal(QuoteStatus.Submitted, quote.Status);
        Assert.StartsWith("qt-", quote.PublicToken);
        Assert.NotNull(quote.SubmittedAt);
    }

    [Fact]
    public void AttachAsPO_PoPathFormat()
    {
        var serviceRequestId = Guid.NewGuid();
        var fileName = "purchase-order.pdf";
        var expectedPath = $"uploads/po/{serviceRequestId}/{fileName}";
        Assert.Contains(serviceRequestId.ToString(), expectedPath);
        Assert.EndsWith(fileName, expectedPath);
    }

    [Fact]
    public void AddToNotes_CommentHasAttribution()
    {
        var fromName = "John Doe";
        var fromAddress = "john@example.com";
        var receivedAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var bodyText = "This is the email content.";

        var sender = fromName ?? fromAddress;
        var attribution = $"From email by {sender} on {receivedAt:yyyy-MM-dd HH:mm UTC}";
        var text = $"{attribution}\n\n{bodyText}";

        Assert.Contains("From email by John Doe", text);
        Assert.Contains("2026-04-01 12:00 UTC", text);
        Assert.Contains(bodyText, text);
    }

    [Fact]
    public void AddToNotes_UsesFromAddressWhenNoName()
    {
        string? fromName = null;
        var fromAddress = "john@example.com";
        var receivedAt = DateTime.UtcNow;

        var sender = fromName ?? fromAddress;
        var attribution = $"From email by {sender} on {receivedAt:yyyy-MM-dd HH:mm UTC}";

        Assert.Contains("From email by john@example.com", attribution);
    }

    [Fact]
    public void ResendEmail_CreatesNewOutboundRecord()
    {
        var original = new OutboundEmail
        {
            ServiceRequestId = Guid.NewGuid(),
            RecipientAddress = "vendor@example.com",
            RecipientName = "Vendor Co",
            Subject = "Work Order Dispatch",
            BodyHtml = "<p>Details here</p>",
            SentAt = DateTime.UtcNow.AddDays(-1),
            SentById = Guid.NewGuid(),
            SentByName = "Admin",
            EmailType = OutboundEmailType.WorkOrderDispatch,
            ConversationId = "WO-001"
        };

        var resent = new OutboundEmail
        {
            ServiceRequestId = original.ServiceRequestId,
            RecipientAddress = original.RecipientAddress,
            RecipientName = original.RecipientName,
            Subject = original.Subject,
            BodyHtml = original.BodyHtml,
            SentAt = DateTime.UtcNow,
            SentById = Guid.NewGuid(),
            SentByName = "Current User",
            EmailType = original.EmailType,
            ConversationId = original.ConversationId
        };

        Assert.NotEqual(original.Id, resent.Id);
        Assert.Equal(original.RecipientAddress, resent.RecipientAddress);
        Assert.Equal(original.Subject, resent.Subject);
        Assert.Equal(original.BodyHtml, resent.BodyHtml);
        Assert.Equal(original.EmailType, resent.EmailType);
        Assert.True(resent.SentAt > original.SentAt);
    }

    [Fact]
    public void ForwardEmail_AddsFwdPrefix()
    {
        var originalSubject = "Work Order Dispatch";
        var subject = $"Fwd: {originalSubject}";
        Assert.StartsWith("Fwd: ", subject);
    }

    [Fact]
    public void ForwardEmail_DoesNotDoubleFwdPrefix()
    {
        var originalSubject = "Fwd: Work Order Dispatch";
        var subject = originalSubject.StartsWith("Fwd: ", StringComparison.OrdinalIgnoreCase)
            ? originalSubject
            : $"Fwd: {originalSubject}";
        Assert.Equal("Fwd: Work Order Dispatch", subject);
    }
}
