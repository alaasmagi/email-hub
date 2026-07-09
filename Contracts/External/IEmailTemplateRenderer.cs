using Contracts.External.Models;
using Domain;

namespace Contracts.External;

public interface IEmailTemplateRenderer
{
    Task<RenderedEmailTemplate> RenderAsync(
        Template template,
        object model,
        CancellationToken cancellationToken = default);
}