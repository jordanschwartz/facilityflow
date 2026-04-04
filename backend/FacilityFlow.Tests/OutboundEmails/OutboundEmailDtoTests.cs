using FacilityFlow.Application.DTOs.OutboundEmails;

namespace FacilityFlow.Tests.OutboundEmails;

public class OutboundEmailDtoTests
{
    [Fact]
    public void OutboundEmailDto_StoresAllFields()
    {
        var id = Guid.NewGuid();
        var srId = Guid.NewGuid();
        var sentAt = DateTime.UtcNow;

        var dto = new OutboundEmailDto(
            id, srId, "vendor@test.com", "Vendor Co",
            "Work Order WO-123", "Preview text...",
            sentAt, "Admin User", "WorkOrderDispatch", 1);

        Assert.Equal(id, dto.Id);
        Assert.Equal(srId, dto.ServiceRequestId);
        Assert.Equal("vendor@test.com", dto.RecipientAddress);
        Assert.Equal("Vendor Co", dto.RecipientName);
        Assert.Equal("Work Order WO-123", dto.Subject);
        Assert.Equal("Preview text...", dto.BodyPreview);
        Assert.Equal(sentAt, dto.SentAt);
        Assert.Equal("Admin User", dto.SentByName);
        Assert.Equal("WorkOrderDispatch", dto.EmailType);
        Assert.Equal(1, dto.AttachmentCount);
    }

    [Fact]
    public void OutboundEmailDetailDto_IncludesFullBodyAndAttachments()
    {
        var id = Guid.NewGuid();
        var attachments = new List<OutboundEmailAttachmentDto>
        {
            new(Guid.NewGuid(), "work-order.pdf", "application/pdf", 12345)
        };

        var dto = new OutboundEmailDetailDto(
            id, Guid.NewGuid(), "vendor@test.com", "Vendor Co",
            "Subject", "<p>Full HTML body</p>",
            DateTime.UtcNow, "Admin", "WorkOrderDispatch",
            "WO-ABC123", attachments);

        Assert.Equal("<p>Full HTML body</p>", dto.BodyHtml);
        Assert.Equal("WO-ABC123", dto.ConversationId);
        Assert.Single(dto.Attachments);
        Assert.Equal("work-order.pdf", dto.Attachments[0].FileName);
    }

    [Fact]
    public void OutboundEmailAttachmentDto_StoresAllFields()
    {
        var id = Guid.NewGuid();
        var dto = new OutboundEmailAttachmentDto(id, "document.pdf", "application/pdf", 54321);

        Assert.Equal(id, dto.Id);
        Assert.Equal("document.pdf", dto.FileName);
        Assert.Equal("application/pdf", dto.ContentType);
        Assert.Equal(54321, dto.FileSize);
    }
}
