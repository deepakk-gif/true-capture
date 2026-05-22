using System.Text.RegularExpressions;

namespace TrueCapture.Modules.Social.Services;

/// <summary>Extracts <c>@username</c> mentions from free text.</summary>
public static partial class Mentions
{
    [GeneratedRegex(@"@([A-Za-z0-9_]{3,64})")]
    private static partial Regex Pattern();

    /// <summary>Distinct, lower-cased usernames mentioned in <paramref name="text"/>.</summary>
    public static IReadOnlyList<string> Extract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return Pattern().Matches(text)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
