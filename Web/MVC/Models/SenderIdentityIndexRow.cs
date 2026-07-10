using Domain;

namespace Web.MVC.Models;

public class SenderIdentityIndexRow
{
    public SenderIdentity SenderIdentity { get; set; } = default!;
    public string ClientServiceName { get; set; } = default!;
}
