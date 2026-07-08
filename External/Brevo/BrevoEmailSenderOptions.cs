namespace External.Brevo;

public class BrevoEmailSenderOptions
{
    public string ApiKey { get; set; } = default!;
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3";
}
