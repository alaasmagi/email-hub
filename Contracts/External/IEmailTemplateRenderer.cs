using Contracts.External.Models;
using Domain;

namespace Contracts.External;

public interface IEmailTemplateRenderer
{
    Task<RenderedEmailTemplate> RenderAsync(
        Template template,
        EmailRenderModel model,
        CancellationToken cancellationToken = default);
}