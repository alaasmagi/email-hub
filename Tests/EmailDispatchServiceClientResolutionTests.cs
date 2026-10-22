using Application;
using Contracts.DataAccess;
using Contracts.External;
using Contracts.External.Models;
using Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public class EmailDispatchServiceClientResolutionTests
{
    // Mirrors the email.Clients rows; the fake repository applies the same "ServiceName == x && IsActive"
    // filter as ClientRepository.GetActiveByServiceNameAsync.
    private readonly List<Client> _clients =
    [
        new() { Id = Guid.NewGuid(), ServiceName = "identity-hub.invoice-service", DisplayName = "IH invoice", IsActive = true },
        new() { Id = Guid.NewGuid(), ServiceName = "identity-hub.master", DisplayName = "IH master", IsActive = true },
        new() { Id = Guid.NewGuid(), ServiceName = "invoice-service", DisplayName = "Invoice service", IsActive = true },
        new() { Id = Guid.NewGuid(), ServiceName = "identity-hub.disabled", DisplayName = "IH disabled", IsActive = false }
    ];

    private readonly Mock<IClientRepository> _clientRepository = new();
    private readonly Mock<ISenderIdentityRepository> _senderIdentityRepository = new();
    private readonly Mock<ITemplateRepository> _templateRepository = new();
    private readonly Mock<IEmailRepository> _emailRepository = new();
    private readonly Mock<IEmailTemplateRenderer> _templateRenderer = new();
    private readonly Mock<IEmailSender> _emailSender = new();

    public EmailDispatchServiceClientResolutionTests()
    {
        _clientRepository
            .Setup(x => x.GetActiveByServiceNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string serviceName) =>
                _clients.FirstOrDefault(c => c.ServiceName == serviceName && c.IsActive));

        _senderIdentityRepository
            .Setup(x => x.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid clientId, string emailType, string tenant) => new SenderIdentity
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                EmailType = emailType,
                Tenant = tenant,
                FromAddress = "noreply@example.com",
                DisplayName = "Example"
            });

        _templateRepository
            .Setup(x => x.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Guid senderIdentityId, string languageCode) => new Template
            {
                Id = Guid.NewGuid(),
                SenderIdentityId = senderIdentityId,
                LanguageCode = languageCode,
                SubjectTemplate = "s",
                HtmlBodyTemplate = "b"
            });

        _emailRepository
            .Setup(x => x.TryCreatePendingAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email email, CancellationToken _) =>
            {
                email.Id = Guid.NewGuid();
                return email;
            });

        _templateRenderer
            .Setup(x => x.RenderAsync(It.IsAny<Template>(), It.IsAny<EmailTemplateModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RenderedEmailTemplate { Subject = "Subject", HtmlBody = "<p>Body</p>" });

        _emailSender
            .Setup(x => x.SendAsync(It.IsAny<OutboundEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Success());
    }

    [Fact]
    public async Task SourceAndTenant_ResolvesTenantScopedClient()
    {
        var expectedClient = _clients.Single(c => c.ServiceName == "identity-hub.invoice-service");

        var result = await CreateService().SendTemplatedEmailAsync(
            CreateRequest(source: "identity-hub", tenant: "invoice-service"));

        Assert.Equal(EmailDispatchOutcome.Sent, result.Outcome);
        _clientRepository.Verify(x => x.GetActiveByServiceNameAsync("identity-hub.invoice-service"), Times.Once);
        _senderIdentityRepository.Verify(
            x => x.GetActiveAsync(expectedClient.Id, "user-verify", "invoice-service"), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SourceWithoutTenant_ResolvesClientBySourceOnly(string tenant)
    {
        var expectedClient = _clients.Single(c => c.ServiceName == "invoice-service");

        var result = await CreateService().SendTemplatedEmailAsync(
            CreateRequest(source: "invoice-service", tenant: tenant, emailType: "invoice"));

        Assert.Equal(EmailDispatchOutcome.Sent, result.Outcome);
        _clientRepository.Verify(x => x.GetActiveByServiceNameAsync("invoice-service"), Times.Once);
        _senderIdentityRepository.Verify(
            x => x.GetActiveAsync(expectedClient.Id, "invoice", tenant), Times.Once);
    }

    [Theory]
    [InlineData("identity-hub", "disabled")] // exists, IsActive = false
    [InlineData("identity-hub", "unknown-tenant")] // no such client
    [InlineData("unknown-source", "")] // no such client, no tenant
    public async Task InactiveOrMissingClient_IsRejectedPermanently(string source, string tenant)
    {
        var request = CreateRequest(source: source, tenant: tenant);

        var result = await CreateService().SendTemplatedEmailAsync(request);

        Assert.Equal(EmailDispatchOutcome.PermanentFailure, result.Outcome);
        Assert.Null(result.EmailId);
        Assert.Contains($"'{request.ClientServiceName}'", result.Error);
        _senderIdentityRepository.Verify(
            x => x.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _emailRepository.Verify(
            x => x.TryCreatePendingAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(
            x => x.SendAsync(It.IsAny<OutboundEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TenantScopedClient_IsNotMatchedBySourceAlone()
    {
        // "identity-hub" by itself is not a client row; the old source-only lookup failed exactly here.
        var result = await CreateService().SendTemplatedEmailAsync(
            CreateRequest(source: "identity-hub", tenant: ""));

        Assert.Equal(EmailDispatchOutcome.PermanentFailure, result.Outcome);
        _clientRepository.Verify(x => x.GetActiveByServiceNameAsync("identity-hub"), Times.Once);
    }

    [Theory]
    [InlineData("identity-hub", "invoice-service", "identity-hub.invoice-service")]
    [InlineData("identity-hub", "master", "identity-hub.master")]
    [InlineData("invoice-service", "", "invoice-service")]
    [InlineData("invoice-service", null, "invoice-service")]
    [InlineData("invoice-service", " ", "invoice-service")]
    public void BuildClientServiceName_JoinsSourceAndTenant(string source, string? tenant, string expected)
    {
        Assert.Equal(expected, SendTemplatedEmailRequest.BuildClientServiceName(source, tenant));
    }

    private EmailDispatchService CreateService()
    {
        return new EmailDispatchService(
            _clientRepository.Object,
            _senderIdentityRepository.Object,
            _templateRepository.Object,
            _emailRepository.Object,
            _templateRenderer.Object,
            _emailSender.Object,
            NullLogger<EmailDispatchService>.Instance);
    }

    private static SendTemplatedEmailRequest CreateRequest(string source, string tenant, string emailType = "user-verify")
    {
        return new SendTemplatedEmailRequest
        {
            MessageId = Guid.NewGuid().ToString(),
            Source = source,
            Tenant = tenant,
            EmailType = emailType,
            ToEmail = "user@example.com",
            LanguageCode = "en",
            Content = new object()
        };
    }
}
