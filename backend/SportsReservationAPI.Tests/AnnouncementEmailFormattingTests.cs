using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class AnnouncementEmailFormattingTests
{
    private const string Url = "https://example.com/desabonnement?email=a%40example.com&sig=abc";

    [Fact]
    public void BuildAnnouncementHtml_GreetsByEncodedFirstName()
    {
        // ASCII name on purpose: WebUtility.HtmlEncode turns Latin-1 letters like "é"
        // into numeric entities (&#233;), which is valid HTML but noisy to assert on.
        var html = MailService.BuildAnnouncementHtml("Zoe <b>", "Salut", Url);

        Assert.Contains("<p>Bonjour Zoe &lt;b&gt;,</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_WithoutName_UsesPlainGreeting()
    {
        var html = MailService.BuildAnnouncementHtml(null, "Salut", Url);

        Assert.Contains("<p>Bonjour,</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_EncodesMarkupInMessage()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "<script>alert(1)</script>", Url);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_SplitsParagraphsAndLineBreaks()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Ligne 1\r\nLigne 2\r\n\r\nParagraphe 2", Url);

        Assert.Contains("<p>Ligne 1<br>Ligne 2</p>", html);
        Assert.Contains("<p>Paragraphe 2</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_LinksUrls()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Voir https://example.com/mon-equipe?a=1&b=2 merci", Url);

        Assert.Contains("<a href=\"https://example.com/mon-equipe?a=1&amp;b=2\">https://example.com/mon-equipe?a=1&amp;b=2</a>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_EndsWithFooter()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Salut", Url);

        Assert.Contains("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", html);
        Assert.Contains("Sport Challenge Police 54", html);
    }

    [Fact]
    public void BuildAnnouncementText_HasGreetingMessageAndFooter()
    {
        var text = MailService.BuildAnnouncementText("Alice", "Ligne 1\nLigne 2", Url);

        Assert.StartsWith("Bonjour Alice,\n\nLigne 1\nLigne 2\n\n", text);
        Assert.Contains("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", text);
        Assert.EndsWith("Sport Challenge Police 54", text);
    }

    [Fact]
    public void BuildAnnouncementHtml_HasUnsubscribeLinkBeforeSignature()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Salut", Url);

        const string sentence = "Vous ne souhaitez plus recevoir d'emails sur nos prochaines éditions ?";
        Assert.Contains(sentence, html);
        Assert.Contains("<a href=\"https://example.com/desabonnement?email=a%40example.com&amp;sig=abc\">Se désabonner</a>", html);
        var idx = html.IndexOf(sentence, StringComparison.Ordinal);
        Assert.True(idx > html.IndexOf("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", StringComparison.Ordinal));
        Assert.True(idx < html.IndexOf("Sport Challenge Police 54", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildAnnouncementText_HasUnsubscribeLine()
    {
        var text = MailService.BuildAnnouncementText("Alice", "Salut", Url);

        Assert.Contains("Se désabonner : " + Url, text);
        Assert.EndsWith("Sport Challenge Police 54", text);
    }

    [Fact]
    public void BuildAnnouncementHtml_EncodesUnsubscribeUrl()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Salut", "https://example.com/x?a=\"><script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&quot;&gt;&lt;script&gt;", html);
    }
}
