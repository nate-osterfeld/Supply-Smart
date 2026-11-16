using SupplySmart.Endpoints.Security;

namespace SupplySmart.Tests;

public class LoginAttemptLimiterTests
{
    [Fact]
    public void ConcurrentAttemptsShareOneLimitAndRecoverAfterTheWindow()
    {
        var clock = new ManualClock();
        var limiter = new LoginAttemptLimiter(clock);
        var accepted = 0;
        Parallel.For(0, 50, attempt =>
        {
            if (limiter.TryAcquire("USER@example.test", out _)) Interlocked.Increment(ref accepted);
        });
        Assert.Equal(10, accepted);
        Assert.False(limiter.TryAcquire(" user@example.test ", out var retry));
        Assert.Equal(60, retry);
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.True(limiter.TryAcquire("user@example.test", out _));
    }

    [Fact]
    public void AccountsCanStillSignInAtCapacityAndTheirWindowSurvivesCleanup()
    {
        var clock = new ManualClock();
        var limiter = new LoginAttemptLimiter(clock);
        for (var i = 0; i < 10_000; i++) Assert.True(limiter.TryAcquire($"user{i}@example.test", out _));
        clock.Advance(TimeSpan.FromSeconds(30));
        for (var i = 1; i < 10; i++) Assert.True(limiter.TryAcquire("user0@example.test", out _));
        for (var i = 0; i < 10; i++) Assert.True(limiter.TryAcquire("overflow@example.test", out _));
        Assert.False(limiter.TryAcquire("overflow@example.test", out _));
        Assert.False(limiter.TryAcquire("user0@example.test", out _));
        clock.Advance(TimeSpan.FromSeconds(31));
        Assert.False(limiter.TryAcquire("overflow@example.test", out _));
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(limiter.TryAcquire("overflow@example.test", out _));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan amount) => now += amount;
    }
}
