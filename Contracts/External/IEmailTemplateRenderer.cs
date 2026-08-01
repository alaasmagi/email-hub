using Contracts.External.Models;
using Domain;

namespace Contracts.External;

public interface IEmailTemplateRenderer
{
    Task<RenderedEmailTemplate> RenderAsync(
        Template template,
        EmailTemplateModel model,
        CancellationToken cancellationToken = default);
}