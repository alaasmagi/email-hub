namespace Web.MVC.Models;

public class ClientOption
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}
