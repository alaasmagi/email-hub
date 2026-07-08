using Base.Domain;

namespace DTO.Web;

public class ClientDto : BaseEntity
{
    public string ServiceName { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}