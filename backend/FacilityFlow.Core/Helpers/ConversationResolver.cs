using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FacilityFlow.Core.Helpers;

public static partial class ConversationResolver
{
    public static string NormalizeSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return string.Empty;

        var normalized = subject;

        // Strip nested Re:/RE:/Fwd:/FW: prefixes
        normalized = PrefixPattern().Replace(normalized, "").Trim();

        // Strip [WO-xxx] brackets
        normalized = WoBracketPattern().Replace(normalized, "").Trim();

        // Strip standalone WO-xxx references (e.g. "Work Order #WO-26-000001 —")
        normalized = WoReferencePattern().Replace(normalized, "").Trim();

        // Clean up leftover separators (— or - at start)
        normalized = LeadingSeparatorPattern().Replace(normalized, "").Trim();

        return normalized;
    }

    public static string GenerateConversationId(string normalizedSubject, Guid serviceRequestId)
    {
        var input = $"{normalizedSubject.ToLowerInvariant()}|{serviceRequestId}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }

    [GeneratedRegex(@"^(\s*(Re|RE|Fwd|FW|Fw)\s*:\s*)+", RegexOptions.None)]
    private static partial Regex PrefixPattern();

    [GeneratedRegex(@"\[WO-[^\]]*\]", RegexOptions.None)]
    private static partial Regex WoBracketPattern();

    [GeneratedRegex(@"Work Order\s+#?WO-\S+\s*[—\-]?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex WoReferencePattern();

    [GeneratedRegex(@"^[—\-]+\s*", RegexOptions.None)]
    private static partial Regex LeadingSeparatorPattern();
}
