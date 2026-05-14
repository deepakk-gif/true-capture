namespace TrueCapture.Infrastructure.Services;

public sealed class FirebaseOptions
{
    /// <summary>Path to the Firebase Admin service-account JSON. Relative paths resolve against `ContentRootPath`.</summary>
    public string ServiceAccountJsonPath { get; set; } = "";

    /// <summary>Optional. If blank, the project id is read from the service-account JSON.</summary>
    public string ProjectId { get; set; } = "";

    /// <summary>Topic every device is auto-subscribed to on register/login.</summary>
    public string DefaultTopic { get; set; } = "all";
}
