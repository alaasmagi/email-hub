using System.Security.Cryptography;
using System.Text;
using Contracts.External;
using Contracts.External.Models;
using Domain;
using RazorLight;

namespace External.RazorLight;

public class RazorEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public RazorEmailTemplateRenderer()
    {
        _engine = new RazorLightEngineBuilder()
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<RenderedEmailTemplate> RenderAsync(
        Template template,
        object model,
        CancellationToken cancellationToken = default)
    {
        var subject = await _engine.CompileRenderStringAsync(
            BuildTemplateKey(template, "subject", template.SubjectTemplate),
            template.SubjectTemplate,
            model);

        var htmlBody = await _engine.CompileRenderStringAsync(
            BuildTemplateKey(template, "body", template.HtmlBodyTemplate),
            template.HtmlBodyTemplate,
            model);

        return new RenderedEmailTemplate
        {
            Subject = subject.Trim(),
            HtmlBody = htmlBody
        };
    }

    private static string BuildTemplateKey(
        Template template,
        string part,
        string content)
    {
        var contentHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(content)));

        return $"email-template:{template.Id}:{template.LanguageCode}:{part}:{contentHash}";
    }
}
