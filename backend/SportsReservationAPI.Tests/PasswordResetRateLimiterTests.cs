using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class PasswordResetRateLimiterTests
{
    [Fact]
    public void TryRegisterIpRequest_UnderLimit_ReturnsTrue()
    {
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 3);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
    }

    [Fact]
    public void TryRegisterIpRequest_OverLimit_ReturnsFalse()
    {
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 3);

        limiter.TryRegisterIpRequest("1.2.3.4");
        limiter.TryRegisterIpRequest("1.2.3.4");
        limiter.TryRegisterIpRequest("1.2.3.4");

        Assert.False(limiter.TryRegisterIpRequest("1.2.3.4"));
    }

    [Fact]
    public void TryRegisterIpRequest_DifferentIps_AreIndependent()
    {
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 1);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.True(limiter.TryRegisterIpRequest("5.6.7.8"));
    }

    [Fact]
    public void TryRegisterIpRequest_AfterWindowExpires_AllowsAgain()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 1, now: () => now);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.False(limiter.TryRegisterIpRequest("1.2.3.4"));

        now = now.AddHours(1).AddMinutes(1);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
    }

    [Fact]
    public void TryRegisterGlobalRequest_OverLimit_ReturnsFalse()
    {
        var limiter = new PasswordResetRateLimiter(maxGlobalPerDay: 2);

        Assert.True(limiter.TryRegisterGlobalRequest());
        Assert.True(limiter.TryRegisterGlobalRequest());
        Assert.False(limiter.TryRegisterGlobalRequest());
    }

    [Fact]
    public void TryRegisterGlobalRequest_AfterWindowExpires_AllowsAgain()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(maxGlobalPerDay: 1, now: () => now);

        Assert.True(limiter.TryRegisterGlobalRequest());
        Assert.False(limiter.TryRegisterGlobalRequest());

        now = now.AddHours(24).AddMinutes(1);

        Assert.True(limiter.TryRegisterGlobalRequest());
    }

    [Fact]
    public void IsEmailInCooldown_BeforeAnyRequest_ReturnsFalse()
    {
        var limiter = new PasswordResetRateLimiter();

        Assert.False(limiter.IsEmailInCooldown("alice@example.com"));
    }

    [Fact]
    public void IsEmailInCooldown_JustAfterRequest_ReturnsTrue()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(emailCooldownMinutes: 15, now: () => now);

        limiter.RecordEmailRequest("alice@example.com");

        Assert.True(limiter.IsEmailInCooldown("alice@example.com"));
    }

    [Fact]
    public void IsEmailInCooldown_AfterCooldownExpires_ReturnsFalse()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(emailCooldownMinutes: 15, now: () => now);

        limiter.RecordEmailRequest("alice@example.com");
        now = now.AddMinutes(16);

        Assert.False(limiter.IsEmailInCooldown("alice@example.com"));
    }
}
