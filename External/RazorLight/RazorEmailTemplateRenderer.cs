using System.Dynamic;
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
        EmailTemplateModel model,
        CancellationToken cancellationToken = default)
    {
        // A template declares `@model <ContentType>` (e.g. InvoiceEmailContent) and reads the content's
        // members straight off Model. So the RazorLight model is the concrete content object itself,
        // not the EmailTemplateModel wrapper. The other two axes the wrapper carries — Fmt (locale
        // formatting) and Branding (tenant identity) — are still reachable, via the ViewBag:
        // `@ViewBag.Fmt.Amount(...)`, `@ViewBag.Branding.DisplayName`.
        var content = (object)model.Content;

        dynamic viewBag = new ExpandoObject();
        viewBag.Fmt = model.Fmt;
        viewBag.Branding = model.Branding;
        viewBag.Locale = model.Locale;

        var subject = await _engine.CompileRenderStringAsync(
            BuildTemplateKey(template, "subject", template.SubjectTemplate),
            template.SubjectTemplate,
            content,
            (ExpandoObject)viewBag);

        var htmlBody = await _engine.CompileRenderStringAsync(
            BuildTemplateKey(template, "body", template.HtmlBodyTemplate),
            template.HtmlBodyTemplate,
            content,
            (ExpandoObject)viewBag);

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
