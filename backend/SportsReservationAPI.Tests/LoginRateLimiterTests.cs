using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class LoginRateLimiterTests
{
    private const string Alice = "alice@example.com";

    [Fact]
    public void TryBeginAttempt_BelowMax_IsAllowed()
    {
        var limiter = new LoginRateLimiter(maxFailures: 3);

        Assert.True(limiter.TryBeginAttempt(Alice));
        Assert.True(limiter.TryBeginAttempt(Alice));
        Assert.True(limiter.TryBeginAttempt(Alice));
    }

    [Fact]
    public void TryBeginAttempt_AtMax_IsRefused()
    {
        var limiter = new LoginRateLimiter(maxFailures: 3);

        for (var i = 0; i < 3; i++) limiter.TryBeginAttempt(Alice);

        Assert.False(limiter.TryBeginAttempt(Alice));
    }

    [Fact]
    public void TryBeginAttempt_IsCaseInsensitiveAndTrimmed()
    {
        var limiter = new LoginRateLimiter(maxFailures: 2);

        limiter.TryBeginAttempt("Alice@Example.com");
        limiter.TryBeginAttempt(" alice@example.com ");

        Assert.False(limiter.TryBeginAttempt("ALICE@EXAMPLE.COM"));
    }

    [Fact]
    public void TryBeginAttempt_DifferentUsernames_AreIndependent()
    {
        var limiter = new LoginRateLimiter(maxFailures: 1);

        limiter.TryBeginAttempt(Alice);

        Assert.True(limiter.TryBeginAttempt("bob@example.com"));
    }

    [Fact]
    public void Reset_ClearsAttempts()
    {
        var limiter = new LoginRateLimiter(maxFailures: 2);

        limiter.TryBeginAttempt(Alice);
        limiter.TryBeginAttempt(Alice);
        limiter.Reset(Alice);

        Assert.True(limiter.TryBeginAttempt(Alice));
    }

    [Fact]
    public void TryBeginAttempt_AfterWindowExpires_IsAllowedAgain()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new LoginRateLimiter(maxFailures: 2, windowMinutes: 15, now: () => now);

        limiter.TryBeginAttempt(Alice);
        limiter.TryBeginAttempt(Alice);
        Assert.False(limiter.TryBeginAttempt(Alice));

        now = now.AddMinutes(16);

        Assert.True(limiter.TryBeginAttempt(Alice));
    }

    [Fact]
    public void TryBeginAttempt_ConcurrentBurst_AllowsExactlyMaxFailures()
    {
        var limiter = new LoginRateLimiter(maxFailures: 10);
        var allowed = 0;

        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 32 }, _ =>
        {
            if (limiter.TryBeginAttempt(Alice))
                Interlocked.Increment(ref allowed);
        });

        Assert.Equal(10, allowed);
    }

    [Fact]
    public void TryBeginAttempt_EvictsExpiredUsernames()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new LoginRateLimiter(maxFailures: 10, windowMinutes: 15, now: () => now);

        for (var i = 0; i < 300; i++)
            limiter.TryBeginAttempt($"user{i}@example.com");
        Assert.True(limiter.TrackedUsernameCount >= 300);

        now = now.AddMinutes(16);
        for (var i = 0; i < 300; i++)
            limiter.TryBeginAttempt("other@example.com");

        Assert.Equal(1, limiter.TrackedUsernameCount);
    }
}
