namespace TrueCapture.Modules.Users.Services;

/// <summary>
/// Shared validation rules for avatar image uploads — used by both the self
/// (<c>UsersController</c>) and admin (<c>AdminUsersController</c>) upload paths.
/// </summary>
public static class AvatarRules
{
    /// <summary>Maximum accepted upload size, in bytes.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

    /// <summary>Storage folder avatars are written under.</summary>
    public const string Folder = "avatars";

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    /// <summary>Returns validation errors, or an empty list when the upload is acceptable.</summary>
    public static IReadOnlyList<string> Validate(string? contentType, long length)
    {
        var errors = new List<string>();

        if (length <= 0)
            errors.Add("The uploaded file is empty.");
        else if (length > MaxBytes)
            errors.Add($"Image must be {MaxBytes / (1024 * 1024)} MB or smaller.");

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
            errors.Add("Image must be a JPEG, PNG, or WebP file.");

        return errors;
    }
}
