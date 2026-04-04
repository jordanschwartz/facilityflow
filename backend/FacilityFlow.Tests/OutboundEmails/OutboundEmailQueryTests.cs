using FacilityFlow.Application.Queries.OutboundEmails;

namespace FacilityFlow.Tests.OutboundEmails;

public class OutboundEmailQueryTests
{
    [Fact]
    public void GeneratePreview_ReturnsNull_WhenBodyIsNull()
    {
        var result = GetOutboundEmailsByServiceRequestQueryHandler.GeneratePreview(null);
        Assert.Null(result);
    }

    [Fact]
    public void GeneratePreview_ReturnsNull_WhenBodyIsEmpty()
    {
        var result = GetOutboundEmailsByServiceRequestQueryHandler.GeneratePreview("");
        Assert.Null(result);
    }

    [Fact]
    public void GeneratePreview_StripsHtmlTags()
    {
        var html = "<p>Hello <strong>World</strong></p>";
        var result = GetOutboundEmailsByServiceRequestQueryHandler.GeneratePreview(html);
        Assert.NotNull(result);
        Assert.DoesNotContain("<", result);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void GeneratePreview_TruncatesAt200Characters()
    {
        var longText = new string('A', 300);
        var html = $"<p>{longText}</p>";
        var result = GetOutboundEmailsByServiceRequestQueryHandler.GeneratePreview(html);
        Assert.NotNull(result);
        Assert.True(result!.Length <= 203); // 200 chars + "..."
        Assert.EndsWith("...", result);
    }

    [Fact]
    public void GeneratePreview_DoesNotTruncate_WhenUnder200Characters()
    {
        var shortText = "Short email body";
        var html = $"<p>{shortText}</p>";
        var result = GetOutboundEmailsByServiceRequestQueryHandler.GeneratePreview(html);
        Assert.NotNull(result);
        Assert.DoesNotContain("...", result);
        Assert.Contains(shortText, result);
    }

    [Fact]
    public void GeneratePreview_CollapsesWhitespace()
    {
        var html = "<p>Hello</p>   <p>World</p>";
        var result = GetOutboundEmailsByServiceRequestQueryHandler.GeneratePreview(html);
        Assert.NotNull(result);
        // Should not have multiple consecutive spaces
        Assert.DoesNotContain("  ", result);
    }
}
