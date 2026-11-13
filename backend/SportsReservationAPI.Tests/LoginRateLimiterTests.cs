using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class LoginRateLimiterTests
{
    [Fact]
    public void IsLockedOut_BelowMaxFailures_ReturnsFalse()
    {
        var limiter = new LoginRateLimiter(maxFailures: 3);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");

        Assert.False(limiter.IsLockedOut("alice@example.com"));
    }

    [Fact]
    public void IsLockedOut_AtMaxFailures_ReturnsTrue()
    {
        var limiter = new LoginRateLimiter(maxFailures: 3);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");

        Assert.True(limiter.IsLockedOut("alice@example.com"));
    }

    [Fact]
    public void IsLockedOut_IsCaseInsensitiveAndTrimmed()
    {
        var limiter = new LoginRateLimiter(maxFailures: 2);

        limiter.RecordFailure("Alice@Example.com");
        limiter.RecordFailure(" alice@example.com ");

        Assert.True(limiter.IsLockedOut("ALICE@EXAMPLE.COM"));
    }

    [Fact]
    public void IsLockedOut_DifferentUsernames_AreIndependent()
    {
        var limiter = new LoginRateLimiter(maxFailures: 1);

        limiter.RecordFailure("alice@example.com");

        Assert.False(limiter.IsLockedOut("bob@example.com"));
    }

    [Fact]
    public void Reset_ClearsFailures()
    {
        var limiter = new LoginRateLimiter(maxFailures: 2);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");
        limiter.Reset("alice@example.com");

        Assert.False(limiter.IsLockedOut("alice@example.com"));
    }

    [Fact]
    public void IsLockedOut_AfterWindowExpires_ReturnsFalse()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new LoginRateLimiter(maxFailures: 2, windowMinutes: 15, now: () => now);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");
        Assert.True(limiter.IsLockedOut("alice@example.com"));

        now = now.AddMinutes(16);

        Assert.False(limiter.IsLockedOut("alice@example.com"));
    }
}
