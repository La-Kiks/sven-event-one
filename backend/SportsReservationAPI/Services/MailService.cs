using System.Net;
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

    public Task<bool> SendActivationEmailAsync(string toEmail, string toName, string activationUrl)
    {
        var toNameHtml = WebUtility.HtmlEncode(toName);
        var text =
            $"Bonjour {toName},\n\n" +
            "Votre equipe a bien ete enregistree. Cliquez sur le lien ci-dessous pour verifier votre email et definir votre mot de passe :\n" +
            $"{activationUrl}\n\n" +
            "Ce lien est valable 7 jours.\n\n" +
            "A bientot,\nSport Challenge Police 54";
        var html =
            $"<p>Bonjour {toNameHtml},</p>" +
            "<p>Votre équipe a bien été enregistrée. Cliquez sur le lien ci-dessous pour vérifier votre email et définir votre mot de passe :</p>" +
            $"<p><a href=\"{activationUrl}\">{activationUrl}</a></p>" +
            "<p>Ce lien est valable 7 jours.</p>" +
            "<p>À bientôt,<br>Sport Challenge Police 54</p>";

        return SendEmailAsync(toEmail, toName, "Activez votre compte - Sport Challenge Police 54", text, html);
    }

    public Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl)
    {
        var toNameHtml = WebUtility.HtmlEncode(toName);
        var text =
            $"Bonjour {toName},\n\n" +
            "Vous avez demande la reinitialisation de votre mot de passe. Cliquez sur le lien ci-dessous pour en definir un nouveau :\n" +
            $"{resetUrl}\n\n" +
            "Ce lien est valable 7 jours. Si vous n'etes pas a l'origine de cette demande, ignorez cet email.\n\n" +
            "A bientot,\nSport Challenge Police 54";
        var html =
            $"<p>Bonjour {toNameHtml},</p>" +
            "<p>Vous avez demandé la réinitialisation de votre mot de passe. Cliquez sur le lien ci-dessous pour en définir un nouveau :</p>" +
            $"<p><a href=\"{resetUrl}\">{resetUrl}</a></p>" +
            "<p>Ce lien est valable 7 jours. Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>" +
            "<p>À bientôt,<br>Sport Challenge Police 54</p>";

        return SendEmailAsync(toEmail, toName, "Réinitialisation de votre mot de passe - Sport Challenge Police 54", text, html);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string text, string html)
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.ApiKey) || string.IsNullOrWhiteSpace(_mailSettings.Domain))
        {
            _logger.LogWarning("Mailgun is not configured (MAILGUN_API_KEY/MAILGUN_DOMAIN missing) - skipping email to {Email}", toEmail);
            return false;
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
                ["subject"] = subject,
                ["text"] = text,
                ["html"] = html
            });

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Mailgun request failed ({StatusCode}) for {Email}: {Body}", response.StatusCode, toEmail, responseBody);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
