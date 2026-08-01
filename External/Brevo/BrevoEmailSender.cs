using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Contracts.External;
using Contracts.External.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace External.Brevo;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoEmailSenderOptions _options;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<BrevoEmailSenderOptions> options,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        OutboundEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            // Misconfiguration, not a transient outage — requeueing would just spin.
            _logger.LogError("Brevo email send failed because BREVO_API_KEY is not configured.");
            return EmailSendResult.Failure("Brevo API key is not configured.");
        }

        _logger.LogInformation(
            "Sending email through Brevo. CorrelationId: {CorrelationId}",
            message.CorrelationId);

        var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("smtp/email"))
        {
            Content = JsonContent.Create(BuildRequest(message))
        };

        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Add("accept", "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var transient = IsTransientStatus(statusCode);

                _logger.LogError(
                    "Brevo email send failed. StatusCode: {StatusCode}, Transient: {Transient}, CorrelationId: {CorrelationId}",
                    statusCode,
                    transient,
                    message.CorrelationId);

                // Note: responseBody is kept out of the returned error/log — a Brevo error can echo
                // request fields, and this result flows into exception paths and GlitchTip.
                return EmailSendResult.Failure(
                    $"Brevo returned {statusCode}.",
                    transient);
            }

            var sendResponse = await response.Content.ReadFromJsonAsync<BrevoSendResponse>(
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Brevo email send completed. ProviderMessageId: {ProviderMessageId}, CorrelationId: {CorrelationId}",
                sendResponse?.MessageId,
                message.CorrelationId);

            return EmailSendResult.Success(sendResponse?.MessageId);
        }
        catch (HttpRequestException exception)
        {
            // Network-level failure — transient, worth a requeue.
            _logger.LogError(
                exception,
                "Brevo email send failed because the HTTP request failed. CorrelationId: {CorrelationId}",
                message.CorrelationId);

            return EmailSendResult.Failure(exception.Message, transient: true);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout — transient.
            _logger.LogError(
                exception,
                "Brevo email send timed out. CorrelationId: {CorrelationId}",
                message.CorrelationId);

            return EmailSendResult.Failure(exception.Message, transient: true);
        }
    }

    // 5xx = Brevo-side outage; 408 request timeout; 429 rate limit → all transient (retry may work).
    // Every other 4xx (bad request, invalid recipient, auth) is permanent.
    private static bool IsTransientStatus(int statusCode)
    {
        return statusCode >= 500 || statusCode == 408 || statusCode == 429;
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}");
    }

    private static BrevoSendRequest BuildRequest(OutboundEmailMessage message)
    {
        return new BrevoSendRequest
        {
            Sender = new BrevoAddress
            {
                Email = message.FromAddress,
                Name = message.FromDisplayName
            },
            To =
            [
                new BrevoAddress
                {
                    Email = message.ToEmail
                }
            ],
            ReplyTo = string.IsNullOrWhiteSpace(message.ReplyTo)
                ? null
                : new BrevoAddress
                {
                    Email = message.ReplyTo
                },
            Subject = message.Subject,
            HtmlContent = message.HtmlBody
        };
    }

    private class BrevoSendRequest
    {
        [JsonPropertyName("sender")]
        public BrevoAddress Sender { get; set; } = default!;

        [JsonPropertyName("to")]
        public List<BrevoAddress> To { get; set; } = [];

        [JsonPropertyName("replyTo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BrevoAddress? ReplyTo { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = default!;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; set; } = default!;
    }

    private class BrevoAddress
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = default!;

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
    }

    private class BrevoSendResponse
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }
    }
}
