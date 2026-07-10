namespace Web.MVC.Models;

public class SenderIdentityOption
{
    public Guid Id { get; set; }
    public string ClientServiceName { get; set; } = default!;
    public string EmailType { get; set; } = default!;
    public string FromAddress { get; set; } = default!;
}
