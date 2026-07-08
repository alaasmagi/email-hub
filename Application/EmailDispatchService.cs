using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using Contracts.External.Models;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application;

public class EmailDispatchService : IEmailDispatchService
{
    private readonly IClientRepository _clientRepository;
    private readonly ISenderIdentityRepository _senderIdentityRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailDispatchService> _logger;

    public EmailDispatchService(
        IClientRepository clientRepository,
        ISenderIdentityRepository senderIdentityRepository,
        ITemplateRepository templateRepository,
        IEmailRepository emailRepository,
        IEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender,
        ILogger<EmailDispatchService> logger)
    {
        _clientRepository = clientRepository;
        _senderIdentityRepository = senderIdentityRepository;
        _templateRepository = templateRepository;
        _emailRepository = emailRepository;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<IMethodResponse<Guid>> SendTemplatedEmailAsync(
        SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            _logger.LogWarning(
                "Email dispatch request rejected: {ValidationError}. CorrelationId: {CorrelationId}",
                validationError,
                request.CorrelationId);

            return Failure(validationError);
        }

        _logger.LogInformation(
            "Email dispatch started. ServiceName: {ServiceName}, EmailType: {EmailType}, LanguageCode: {LanguageCode}, CorrelationId: {CorrelationId}",
            request.ServiceName,
            request.EmailType,
            request.LanguageCode,
            request.CorrelationId);

        var client = await _clientRepository.GetActiveByServiceNameAsync(request.ServiceName);
        if (client is null)
        {
            _logger.LogWarning(
                "Email dispatch failed because active client was not found. ServiceName: {ServiceName}, CorrelationId: {CorrelationId}",
                request.ServiceName,
                request.CorrelationId);

            return Failure($"Active client '{request.ServiceName}' was not found.");
        }

        var senderIdentity = await _senderIdentityRepository.GetActiveAsync(
            client.Id,
            request.EmailType);

        if (senderIdentity is null)
        {
            _logger.LogError(
                "Email dispatch failed because active sender identity was not found. ServiceName: {ServiceName}, EmailType: {EmailType}, ClientId: {ClientId}, CorrelationId: {CorrelationId}",
                request.ServiceName,
                request.EmailType,
                client.Id,
                request.CorrelationId);

            return Failure($"Active sender identity '{request.ServiceName}/{request.EmailType}' was not found.");
        }

        var languageCode = NormalizeLanguageCode(request.LanguageCode);

        var template =
            await _templateRepository.GetActiveAsync(senderIdentity.Id, languageCode)
            ?? await _templateRepository.GetActiveAsync(senderIdentity.Id, "en");

        if (template is null)
        {
            _logger.LogError(
                "Email dispatch failed because active template was not found. ServiceName: {ServiceName}, EmailType: {EmailType}, SenderIdentityId: {SenderIdentityId}, LanguageCode: {LanguageCode}, CorrelationId: {CorrelationId}",
                request.ServiceName,
                request.EmailType,
                senderIdentity.Id,
                languageCode,
                request.CorrelationId);

            return Failure($"Active template '{request.ServiceName}/{request.EmailType}/{languageCode}' was not found.");
        }

        var renderModel = new EmailRenderModel
        {
            ServiceName = request.ServiceName,
            EmailType = request.EmailType,
            ToEmail = request.ToEmail,
            LanguageCode = languageCode,
            Variables = request.Variables
        };

        var rendered = await _templateRenderer.RenderAsync(
            template,
            renderModel,
            cancellationToken);

        var email = new Email
        {
            ServiceName = request.ServiceName,
            EmailType = request.EmailType,
            ToRecipients = request.ToEmail,
            ReplyTo = senderIdentity.ReplyTo,
            Subject = rendered.Subject,
            Status = EEmailStatus.Pending
        };

        await _emailRepository.CreateAsync(email);

        _logger.LogInformation(
            "Email record created. EmailId: {EmailId}, ServiceName: {ServiceName}, EmailType: {EmailType}, CorrelationId: {CorrelationId}",
            email.Id,
            request.ServiceName,
            request.EmailType,
            request.CorrelationId);

        var sendResult = await _emailSender.SendAsync(
            new OutboundEmailMessage
            {
                FromAddress = senderIdentity.FromAddress,
                FromDisplayName = senderIdentity.DisplayName,
                ReplyTo = senderIdentity.ReplyTo,
                ToEmail = request.ToEmail,
                Subject = rendered.Subject,
                HtmlBody = rendered.HtmlBody,
                CorrelationId = request.CorrelationId
            },
            cancellationToken);

        email.Status = sendResult.Successful
            ? EEmailStatus.Sent
            : EEmailStatus.Failed;

        if (sendResult.Successful)
        {
            email.SentAt = DateTime.UtcNow;
        }

        await _emailRepository.UpdateAsync(email.Id, email, null, Guid.Empty);

        if (sendResult.Successful)
        {
            _logger.LogInformation(
                "Email dispatch completed. EmailId: {EmailId}, ProviderMessageId: {ProviderMessageId}, CorrelationId: {CorrelationId}",
                email.Id,
                sendResult.ProviderMessageId,
                request.CorrelationId);
        }
        else
        {
            _logger.LogError(
                "Email dispatch failed. EmailId: {EmailId}, CorrelationId: {CorrelationId}, ErrorMessage: {ErrorMessage}",
                email.Id,
                request.CorrelationId,
                sendResult.ErrorMessage);
        }

        return sendResult.Successful
            ? Success(email.Id)
            : Failure(sendResult.ErrorMessage ?? "Email provider failed to send message.");
    }

    private static string? Validate(SendTemplatedEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return "Service name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.EmailType))
        {
            return "Email type is required.";
        }

        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return "Recipient email is required.";
        }

        return null;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static IMethodResponse<Guid> Success(Guid value)
    {
        return MethodResponse<Guid>.Success(value);
    }

    private static IMethodResponse<Guid> Failure(string message)
    {
        return MethodResponse<Guid>.Failure(new Error("email.dispatch.failed", message));
    }
}
