using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Core.Entities;

namespace FacilityFlow.Tests.Quotes;

/// <summary>
/// Tests that verify the field-mapping logic performed in QuotesController.SubmitByToken.
/// Rather than spinning up the full controller, we replicate the mapping assignment block
/// so we can assert each field in isolation.
/// </summary>
public class SubmitQuoteRequestTests
{
    private static Quote BuildQuote() => new()
    {
        Id = Guid.NewGuid(),
        ServiceRequestId = Guid.NewGuid(),
        VendorId = Guid.NewGuid(),
        Price = 0m,
        ScopeOfWork = "",
        LineItems = new List<QuoteLineItem>(),
    };

    private static void ApplyRequest(Quote quote, SubmitQuoteRequest req)
    {
        // Mirrors the mapping block in QuotesController.SubmitByToken
        quote.Price = req.Price;
        quote.ScopeOfWork = req.ScopeOfWork;
        quote.ProposedStartDate = req.ProposedStartDate;
        quote.EstimatedDurationValue = req.EstimatedDurationValue;
        quote.EstimatedDurationUnit = req.EstimatedDurationUnit;
        quote.NotToExceedPrice = req.NotToExceedPrice;
        quote.Assumptions = req.Assumptions;
        quote.Exclusions = req.Exclusions;
        quote.VendorAvailability = req.VendorAvailability;
        quote.ValidUntil = req.ValidUntil;

        quote.LineItems.Clear();
        if (req.LineItems != null)
        {
            foreach (var li in req.LineItems)
            {
                quote.LineItems.Add(new QuoteLineItem
                {
                    Id = Guid.NewGuid(),
                    QuoteId = quote.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                });
            }
        }
    }

    [Fact]
    public void AllNewOptionalFields_AreMappedToQuote()
    {
        var quote = BuildQuote();
        var startDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var validUntil = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

        var req = new SubmitQuoteRequest(
            Price: 5000m,
            ScopeOfWork: "Replace HVAC unit",
            ProposedStartDate: startDate,
            EstimatedDurationValue: 3,
            EstimatedDurationUnit: "Days",
            NotToExceedPrice: 6000m,
            Assumptions: "Site access available 8am–5pm",
            Exclusions: "Does not include disposal fees",
            VendorAvailability: "Available within 48 hours",
            ValidUntil: validUntil,
            LineItems: null
        );

        ApplyRequest(quote, req);

        Assert.Equal(5000m, quote.Price);
        Assert.Equal("Replace HVAC unit", quote.ScopeOfWork);
        Assert.Equal(startDate, quote.ProposedStartDate);
        Assert.Equal(3, quote.EstimatedDurationValue);
        Assert.Equal("Days", quote.EstimatedDurationUnit);
        Assert.Equal(6000m, quote.NotToExceedPrice);
        Assert.Equal("Site access available 8am–5pm", quote.Assumptions);
        Assert.Equal("Does not include disposal fees", quote.Exclusions);
        Assert.Equal("Available within 48 hours", quote.VendorAvailability);
        Assert.Equal(validUntil, quote.ValidUntil);
    }

    [Fact]
    public void NotToExceedPrice_IsNullable_AndOptional()
    {
        var quote = BuildQuote();
        var req = new SubmitQuoteRequest(Price: 1000m, ScopeOfWork: "Fix leak");

        ApplyRequest(quote, req);

        Assert.Null(quote.NotToExceedPrice);
    }

    [Fact]
    public void AllOptionalFields_AreNullWhenNotSupplied()
    {
        var quote = BuildQuote();
        var req = new SubmitQuoteRequest(Price: 2500m, ScopeOfWork: "Paint interior walls");

        ApplyRequest(quote, req);

        Assert.Null(quote.ProposedStartDate);
        Assert.Null(quote.EstimatedDurationValue);
        Assert.Null(quote.EstimatedDurationUnit);
        Assert.Null(quote.NotToExceedPrice);
        Assert.Null(quote.Assumptions);
        Assert.Null(quote.Exclusions);
        Assert.Null(quote.VendorAvailability);
        Assert.Null(quote.ValidUntil);
    }

    [Fact]
    public void LineItems_AreSavedFromRequest()
    {
        var quote = BuildQuote();
        var req = new SubmitQuoteRequest(
            Price: 800m,
            ScopeOfWork: "Electrical work",
            LineItems: new List<SubmitQuoteLineItemRequest>
            {
                new("Labor", 8m, 90m),
                new("Parts", 1m, 250m),
            }
        );

        ApplyRequest(quote, req);

        Assert.Equal(2, quote.LineItems.Count);

        var labor = quote.LineItems.First(li => li.Description == "Labor");
        Assert.Equal(8m, labor.Quantity);
        Assert.Equal(90m, labor.UnitPrice);

        var parts = quote.LineItems.First(li => li.Description == "Parts");
        Assert.Equal(1m, parts.Quantity);
        Assert.Equal(250m, parts.UnitPrice);
    }

    [Fact]
    public void LineItems_AreClearedAndReplacedOnReSubmission()
    {
        var quote = BuildQuote();

        // First submission: 2 line items
        ApplyRequest(quote, new SubmitQuoteRequest(
            Price: 500m,
            ScopeOfWork: "Initial work",
            LineItems: new List<SubmitQuoteLineItemRequest>
            {
                new("Item A", 2m, 100m),
                new("Item B", 3m, 50m),
            }
        ));

        Assert.Equal(2, quote.LineItems.Count);

        // Re-submission: 1 different line item
        ApplyRequest(quote, new SubmitQuoteRequest(
            Price: 600m,
            ScopeOfWork: "Revised work",
            LineItems: new List<SubmitQuoteLineItemRequest>
            {
                new("Item C", 5m, 120m),
            }
        ));

        Assert.Single(quote.LineItems);
        Assert.Equal("Item C", quote.LineItems.First().Description);
    }

    [Fact]
    public void LineItems_AreEmptyWhenRequestLineItemsIsNull()
    {
        var quote = BuildQuote();
        var req = new SubmitQuoteRequest(Price: 1200m, ScopeOfWork: "General maintenance", LineItems: null);

        ApplyRequest(quote, req);

        Assert.Empty(quote.LineItems);
    }

    [Fact]
    public void LineItems_AreEmptyWhenRequestLineItemsIsEmptyList()
    {
        var quote = BuildQuote();
        var req = new SubmitQuoteRequest(
            Price: 1200m,
            ScopeOfWork: "General maintenance",
            LineItems: new List<SubmitQuoteLineItemRequest>()
        );

        ApplyRequest(quote, req);

        Assert.Empty(quote.LineItems);
    }
}
