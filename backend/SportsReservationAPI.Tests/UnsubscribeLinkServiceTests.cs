using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Tests;

public class UnsubscribeLinkServiceTests
{
    private static UnsubscribeLinkService Create(string jwtKey = "unit-test-key")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Jwt:Key", jwtKey } })
            .Build();
        return new UnsubscribeLinkService(config, Options.Create(new ApiSettings { FrontendBaseUrl = "https://example.com/" }));
    }

    [Fact]
    public void Verify_AcceptsOwnSignature()
    {
        var svc = Create();
        Assert.True(svc.Verify("alice@example.com", svc.Sign("alice@example.com")));
    }

    [Fact]
    public void Verify_IgnoresCaseAndWhitespace()
    {
        var svc = Create();
        Assert.True(svc.Verify("  Alice@Example.com ", svc.Sign("alice@example.com")));
    }

    [Fact]
    public void Verify_RejectsOtherEmail()
    {
        var svc = Create();
        Assert.False(svc.Verify("bob@example.com", svc.Sign("alice@example.com")));
    }

    [Fact]
    public void Verify_RejectsTamperedOrMalformedSig()
    {
        var svc = Create();
        var sig = svc.Sign("alice@example.com");
        Assert.False(svc.Verify("alice@example.com", sig + "x"));
        Assert.False(svc.Verify("alice@example.com", "not base64 !!"));
        Assert.False(svc.Verify("alice@example.com", ""));
        Assert.False(svc.Verify("alice@example.com", null));
        Assert.False(svc.Verify(null, sig));
    }

    [Fact]
    public void Sign_DependsOnJwtKey()
    {
        Assert.NotEqual(Create().Sign("alice@example.com"), Create("other-key").Sign("alice@example.com"));
    }

    [Fact]
    public void BuildUrl_EncodesEmailAndTrimsBaseSlash()
    {
        var svc = Create();
        var url = svc.BuildUrl("A+b@Example.com");
        var prefix = "https://example.com/desabonnement?email=a%2Bb%40example.com&sig=";
        Assert.StartsWith(prefix, url);
        Assert.Equal(svc.Sign("a+b@example.com"), url.Substring(prefix.Length));
    }
}
