namespace Contracts.External.Models;

public class KeycloakEmailEvent
{
    public string EventType { get; set; } = default!;
    public string EventSource { get; set; } = default!;
    public string RealmName { get; set; } = default!;
    public string Timestamp { get; set; } = default!;
    public KeycloakEmailEventPayload Payload { get; set; } = default!;
}
