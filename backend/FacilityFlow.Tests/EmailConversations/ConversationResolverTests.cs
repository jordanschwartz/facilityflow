using FacilityFlow.Core.Helpers;

namespace FacilityFlow.Tests.EmailConversations;

public class ConversationResolverTests
{
    [Theory]
    [InlineData("Re: Plumbing Fix", "Plumbing Fix")]
    [InlineData("RE: Plumbing Fix", "Plumbing Fix")]
    [InlineData("Fwd: Plumbing Fix", "Plumbing Fix")]
    [InlineData("FW: Plumbing Fix", "Plumbing Fix")]
    [InlineData("Fw: Plumbing Fix", "Plumbing Fix")]
    public void NormalizeSubject_StripsSinglePrefix(string input, string expected)
    {
        Assert.Equal(expected, ConversationResolver.NormalizeSubject(input));
    }

    [Theory]
    [InlineData("Re: Re: Fwd: Plumbing Fix", "Plumbing Fix")]
    [InlineData("RE: RE: FW: Plumbing Fix", "Plumbing Fix")]
    [InlineData("Fwd: Re: RE: Plumbing Fix", "Plumbing Fix")]
    public void NormalizeSubject_StripsNestedPrefixes(string input, string expected)
    {
        Assert.Equal(expected, ConversationResolver.NormalizeSubject(input));
    }

    [Theory]
    [InlineData("[WO-26-000001] Plumbing Fix", "Plumbing Fix")]
    [InlineData("RE: RE: FW: [WO-26-000001] Plumbing Fix", "Plumbing Fix")]
    [InlineData("Fwd: Proposal for Roof Repair [WO-26-000042]", "Proposal for Roof Repair")]
    public void NormalizeSubject_StripsWoBrackets(string input, string expected)
    {
        Assert.Equal(expected, ConversationResolver.NormalizeSubject(input));
    }

    [Fact]
    public void NormalizeSubject_StripsWorkOrderReference()
    {
        var result = ConversationResolver.NormalizeSubject("Re: Work Order #WO-26-000001 — Plumbing Fix");
        Assert.Equal("Plumbing Fix", result);
    }

    [Theory]
    [InlineData("  Plumbing Fix  ", "Plumbing Fix")]
    [InlineData("Re:   Plumbing Fix  ", "Plumbing Fix")]
    public void NormalizeSubject_TrimsWhitespace(string input, string expected)
    {
        Assert.Equal(expected, ConversationResolver.NormalizeSubject(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NormalizeSubject_HandlesEmptyAndNull(string? input)
    {
        Assert.Equal(string.Empty, ConversationResolver.NormalizeSubject(input!));
    }

    [Fact]
    public void GenerateConversationId_IsDeterministic()
    {
        var srId = Guid.NewGuid();
        var id1 = ConversationResolver.GenerateConversationId("Plumbing Fix", srId);
        var id2 = ConversationResolver.GenerateConversationId("Plumbing Fix", srId);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateConversationId_DiffersForDifferentSubjects()
    {
        var srId = Guid.NewGuid();
        var id1 = ConversationResolver.GenerateConversationId("Plumbing Fix", srId);
        var id2 = ConversationResolver.GenerateConversationId("Roof Repair", srId);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GenerateConversationId_DiffersForDifferentServiceRequests()
    {
        var id1 = ConversationResolver.GenerateConversationId("Plumbing Fix", Guid.NewGuid());
        var id2 = ConversationResolver.GenerateConversationId("Plumbing Fix", Guid.NewGuid());

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GenerateConversationId_Returns16CharHex()
    {
        var id = ConversationResolver.GenerateConversationId("Test", Guid.NewGuid());

        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }
}
