using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess;

public class ClientEntity : BaseEntityWithMeta
{
    [MaxLength(64)]
    public string ServiceName { get; set; } = default!;
    [MaxLength(64)]
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}