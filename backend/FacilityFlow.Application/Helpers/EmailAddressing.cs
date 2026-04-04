using System.Text.RegularExpressions;

namespace FacilityFlow.Application.Helpers;

public static partial class EmailAddressing
{
    private const string DefaultDomain = "oncallfm.com";

    public static string GetReplyToAddress(string workOrderNumber, string domain = DefaultDomain)
        => $"reply+{workOrderNumber}@{domain}";

    public static string? ParseWorkOrderNumber(string replyToAddress)
    {
        var match = ReplyToPattern().Match(replyToAddress);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? ParseWorkOrderNumberFromSubject(string subject)
    {
        var match = SubjectPattern().Match(subject);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"^reply\+(.+)@", RegexOptions.IgnoreCase)]
    private static partial Regex ReplyToPattern();

    [GeneratedRegex(@"\[([A-Z]+-\d+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex SubjectPattern();
}
