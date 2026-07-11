using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;

namespace SportsReservationAPI.Services;

public class MailService
{
    private readonly HttpClient _httpClient;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<MailService> _logger;

    public MailService(HttpClient httpClient, IOptions<ApiSettings> apiSettings, ILogger<MailService> logger)
    {
        _httpClient = httpClient;
        _mailSettings = apiSettings.Value.Mail;
        _logger = logger;
    }

    public async Task SendActivationEmailAsync(string toEmail, string toName, string activationUrl)
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.ApiKey) || string.IsNullOrWhiteSpace(_mailSettings.Domain))
        {
            _logger.LogWarning("Mailgun is not configured (MAILGUN_API_KEY/MAILGUN_DOMAIN missing) - skipping activation email to {Email}", toEmail);
            return;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_mailSettings.BaseUrl}/v3/{_mailSettings.Domain}/messages");
            var authBytes = Encoding.UTF8.GetBytes($"api:{_mailSettings.ApiKey}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var fromName = string.IsNullOrWhiteSpace(_mailSettings.FromName) ? "Sport Challenge Police 54" : _mailSettings.FromName;
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = $"{fromName} <{_mailSettings.FromAddress}>",
                ["to"] = $"{toName} <{toEmail}>",
                ["subject"] = "Activez votre compte - Sport Challenge Police 54",
                ["text"] =
                    $"Bonjour {toName},\n\n" +
                    "Votre equipe a bien ete enregistree. Cliquez sur le lien ci-dessous pour verifier votre email et definir votre mot de passe :\n" +
                    $"{activationUrl}\n\n" +
                    "Ce lien est valable 7 jours.\n\n" +
                    "A bientot,\nSport Challenge Police 54",
                ["html"] =
                    $"<p>Bonjour {toName},</p>" +
                    "<p>Votre équipe a bien été enregistrée. Cliquez sur le lien ci-dessous pour vérifier votre email et définir votre mot de passe :</p>" +
                    $"<p><a href=\"{activationUrl}\">{activationUrl}</a></p>" +
                    "<p>Ce lien est valable 7 jours.</p>" +
                    "<p>À bientôt,<br>Sport Challenge Police 54</p>"
            });

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Mailgun request failed ({StatusCode}) for {Email}: {Body}", response.StatusCode, toEmail, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send activation email to {Email}", toEmail);
        }
    }
}
