using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class AnnouncementEmailFormattingTests
{
    [Fact]
    public void BuildAnnouncementHtml_GreetsByEncodedFirstName()
    {
        // ASCII name on purpose: WebUtility.HtmlEncode turns Latin-1 letters like "é"
        // into numeric entities (&#233;), which is valid HTML but noisy to assert on.
        var html = MailService.BuildAnnouncementHtml("Zoe <b>", "Salut");

        Assert.Contains("<p>Bonjour Zoe &lt;b&gt;,</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_WithoutName_UsesPlainGreeting()
    {
        var html = MailService.BuildAnnouncementHtml(null, "Salut");

        Assert.Contains("<p>Bonjour,</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_EncodesMarkupInMessage()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_SplitsParagraphsAndLineBreaks()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Ligne 1\r\nLigne 2\r\n\r\nParagraphe 2");

        Assert.Contains("<p>Ligne 1<br>Ligne 2</p>", html);
        Assert.Contains("<p>Paragraphe 2</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_LinksUrls()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Voir https://example.com/mon-equipe?a=1&b=2 merci");

        Assert.Contains("<a href=\"https://example.com/mon-equipe?a=1&amp;b=2\">https://example.com/mon-equipe?a=1&amp;b=2</a>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_EndsWithFooter()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Salut");

        Assert.Contains("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", html);
        Assert.Contains("Sport Challenge Police 54", html);
    }

    [Fact]
    public void BuildAnnouncementText_HasGreetingMessageAndFooter()
    {
        var text = MailService.BuildAnnouncementText("Alice", "Ligne 1\nLigne 2");

        Assert.StartsWith("Bonjour Alice,\n\nLigne 1\nLigne 2\n\n", text);
        Assert.Contains("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", text);
        Assert.EndsWith("Sport Challenge Police 54", text);
    }
}
