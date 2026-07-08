using Base.Contracts.DTO;
using Contracts.External.Models;

namespace Contracts.Application;

public interface IKeycloakEmailEventMapper
{
    IMethodResponse<SendTemplatedEmailRequest> Map(KeycloakEmailEvent emailEvent);
}
