using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.Quotes;

namespace FacilityFlow.Tests.Quotes;

public class QuoteDtoTests
{
    private static QuoteDto BuildDto(
        DateTime? proposedStartDate = null,
        int? estimatedDurationValue = null,
        string? estimatedDurationUnit = null,
        decimal? notToExceedPrice = null,
        string? assumptions = null,
        string? exclusions = null,
        string? vendorAvailability = null,
        DateTime? validUntil = null,
        List<QuoteLineItemDto>? lineItems = null) =>
        new QuoteDto(
            Id: Guid.NewGuid(),
            ServiceRequestId: Guid.NewGuid(),
            VendorId: Guid.NewGuid(),
            Price: 1000m,
            ScopeOfWork: "Some work",
            Status: "Submitted",
            PublicToken: "qt-abc123",
            SubmittedAt: DateTime.UtcNow,
            Vendor: new VendorSummaryDto(Guid.NewGuid(), "Acme HVAC", new List<string> { "HVAC" }, 4.5m),
            Attachments: new List<AttachmentDto>(),
            ProposedStartDate: proposedStartDate,
            EstimatedDurationValue: estimatedDurationValue,
            EstimatedDurationUnit: estimatedDurationUnit,
            NotToExceedPrice: notToExceedPrice,
            Assumptions: assumptions,
            Exclusions: exclusions,
            VendorAvailability: vendorAvailability,
            ValidUntil: validUntil,
            LineItems: lineItems ?? new List<QuoteLineItemDto>()
        );

    [Fact]
    public void NewFields_AreNullableAndDefaultToNull()
    {
        var dto = BuildDto();

        Assert.Null(dto.ProposedStartDate);
        Assert.Null(dto.EstimatedDurationValue);
        Assert.Null(dto.EstimatedDurationUnit);
        Assert.Null(dto.NotToExceedPrice);
        Assert.Null(dto.Assumptions);
        Assert.Null(dto.Exclusions);
        Assert.Null(dto.VendorAvailability);
        Assert.Null(dto.ValidUntil);
    }

    [Fact]
    public void LineItems_DefaultsToEmptyList()
    {
        var dto = BuildDto();

        Assert.NotNull(dto.LineItems);
        Assert.Empty(dto.LineItems);
    }

    [Fact]
    public void AllNewFields_ArePopulatedCorrectly()
    {
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var valid = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var lineItems = new List<QuoteLineItemDto>
        {
            new(Guid.NewGuid(), "Labor", 8m, 90m, 720m),
        };

        var dto = BuildDto(
            proposedStartDate: start,
            estimatedDurationValue: 2,
            estimatedDurationUnit: "Days",
            notToExceedPrice: 1500m,
            assumptions: "Site is accessible",
            exclusions: "Excludes permits",
            vendorAvailability: "Within 24 hours",
            validUntil: valid,
            lineItems: lineItems
        );

        Assert.Equal(start, dto.ProposedStartDate);
        Assert.Equal(2, dto.EstimatedDurationValue);
        Assert.Equal("Days", dto.EstimatedDurationUnit);
        Assert.Equal(1500m, dto.NotToExceedPrice);
        Assert.Equal("Site is accessible", dto.Assumptions);
        Assert.Equal("Excludes permits", dto.Exclusions);
        Assert.Equal("Within 24 hours", dto.VendorAvailability);
        Assert.Equal(valid, dto.ValidUntil);
        Assert.Single(dto.LineItems);
        Assert.Equal(720m, dto.LineItems[0].Total);
    }

    [Fact]
    public void LineItems_CanContainMultipleItems()
    {
        var lineItems = new List<QuoteLineItemDto>
        {
            new(Guid.NewGuid(), "Labor", 4m, 100m, 400m),
            new(Guid.NewGuid(), "Parts", 2m, 50m, 100m),
            new(Guid.NewGuid(), "Disposal", 1m, 75m, 75m),
        };

        var dto = BuildDto(lineItems: lineItems);

        Assert.Equal(3, dto.LineItems.Count);
        Assert.Equal(575m, dto.LineItems.Sum(li => li.Total));
    }

    [Fact]
    public void NotToExceedPrice_AcceptsDecimalPrecision()
    {
        var dto = BuildDto(notToExceedPrice: 9999.99m);

        Assert.Equal(9999.99m, dto.NotToExceedPrice);
    }
}
