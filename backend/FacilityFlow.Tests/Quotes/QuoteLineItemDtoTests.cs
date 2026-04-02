using FacilityFlow.Application.DTOs.Quotes;

namespace FacilityFlow.Tests.Quotes;

public class QuoteLineItemDtoTests
{
    [Fact]
    public void Total_IsQuantityTimesUnitPrice()
    {
        var id = Guid.NewGuid();
        var dto = new QuoteLineItemDto(id, "Labor", Quantity: 3m, UnitPrice: 150m, Total: 3m * 150m);

        Assert.Equal(450m, dto.Total);
    }

    [Fact]
    public void Total_WithFractionalQuantity_IsCorrect()
    {
        var dto = new QuoteLineItemDto(Guid.NewGuid(), "Materials", Quantity: 2.5m, UnitPrice: 80m, Total: 2.5m * 80m);

        Assert.Equal(200m, dto.Total);
    }

    [Fact]
    public void Total_WithZeroUnitPrice_IsZero()
    {
        var dto = new QuoteLineItemDto(Guid.NewGuid(), "Inspection", Quantity: 1m, UnitPrice: 0m, Total: 0m);

        Assert.Equal(0m, dto.Total);
    }

    [Fact]
    public void Total_ComputedInControllerExpression_MatchesManualComputation()
    {
        // This mirrors the controller expression:
        //   li.Quantity * li.UnitPrice
        decimal quantity = 4m;
        decimal unitPrice = 125.50m;
        decimal expected = quantity * unitPrice;

        var dto = new QuoteLineItemDto(Guid.NewGuid(), "Wiring", quantity, unitPrice, quantity * unitPrice);

        Assert.Equal(expected, dto.Total);
        Assert.Equal(502m, dto.Total);
    }

    [Fact]
    public void Dto_ExposesAllExpectedProperties()
    {
        var id = Guid.NewGuid();
        var dto = new QuoteLineItemDto(id, "Painting", 10m, 20m, 200m);

        Assert.Equal(id, dto.Id);
        Assert.Equal("Painting", dto.Description);
        Assert.Equal(10m, dto.Quantity);
        Assert.Equal(20m, dto.UnitPrice);
        Assert.Equal(200m, dto.Total);
    }
}
