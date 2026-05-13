namespace TrueCapture.Infrastructure.Services;

public sealed class EmailOptions
{
    public string  Host         { get; set; } = "";
    public int     Port         { get; set; } = 587;
    public bool    UseStartTls  { get; set; } = true;
    public string  Username     { get; set; } = "";
    public string  Password     { get; set; } = "";
    public string  FromAddress  { get; set; } = "no-reply@truecapture.app";
    public string  FromName     { get; set; } = "True Capture";
}
