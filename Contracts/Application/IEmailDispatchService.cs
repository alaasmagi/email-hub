using Base.Contracts.DTO;
using Contracts.External.Models;

namespace Contracts.Application;

public interface IEmailDispatchService
{
    Task<IMethodResponse<Guid>> SendTemplatedEmailAsync(SendTemplatedEmailRequest request, 
        CancellationToken cancellationToken = default);
}