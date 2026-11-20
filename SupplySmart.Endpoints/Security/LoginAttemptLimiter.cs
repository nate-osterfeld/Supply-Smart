namespace SupplySmart.Endpoints.Security;

// Per-process, per-email fixed window. No untrusted proxy headers or shared-IP lockouts.
public sealed class LoginAttemptLimiter(TimeProvider timeProvider)
{
    private const int MaxAttempts = 10;
    private const int MaxTrackedAccounts = 10_000;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly object gate = new();
    private readonly Dictionary<string, AttemptWindow> accounts = new(StringComparer.OrdinalIgnoreCase);
    // At capacity, new addresses share bounded counters instead of stopping every new login.
    private readonly AttemptWindow[] overflow = new AttemptWindow[1024];
    private DateTimeOffset nextCleanup = DateTimeOffset.MinValue;

    public bool TryAcquire(string email, out int retryAfterSeconds)
    {
        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            if (now >= nextCleanup)
            {
                foreach (var expiredKey in accounts.Where(pair => pair.Value.ResetAt <= now).Select(pair => pair.Key).ToArray())
                    accounts.Remove(expiredKey);
                nextCleanup = now + Window;
            }

            var key = email.Trim();
            var tracked = accounts.TryGetValue(key, out var window);
            var bucket = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(key) % (uint)overflow.Length);
            var useOverflow = !tracked && (accounts.Count >= MaxTrackedAccounts || overflow[bucket].ResetAt > now);
            if (useOverflow) window = overflow[bucket];
            if (window.ResetAt <= now) window = new AttemptWindow(now + Window, 0);

            if (window.Attempts >= MaxAttempts)
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((window.ResetAt - now).TotalSeconds));
                return false;
            }

            window = window with { Attempts = window.Attempts + 1 };
            if (useOverflow) overflow[bucket] = window;
            else accounts[key] = window;
            retryAfterSeconds = 0;
            return true;
        }
    }

    private readonly record struct AttemptWindow(DateTimeOffset ResetAt, int Attempts);
}
