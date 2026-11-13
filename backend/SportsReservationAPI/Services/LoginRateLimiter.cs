using System.Collections.Concurrent;

namespace SportsReservationAPI.Services
{
    // Registered as a singleton (Program.cs). Counts FAILED login attempts per
    // username in a sliding window; once maxFailures is reached the username is
    // locked out (even for a correct password) until old failures age out.
    // Per-username rather than per-IP: the test suite logs in many times from
    // one address, and it protects the admin account regardless of how many
    // IPs an attacker uses. In-memory only, like PasswordResetRateLimiter.
    public class LoginRateLimiter
    {
        private readonly int _maxFailures;
        private readonly TimeSpan _window;
        private readonly Func<DateTime> _now;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _failuresByUsername = new();

        public LoginRateLimiter(int maxFailures = 10, int windowMinutes = 15, Func<DateTime>? now = null)
        {
            _maxFailures = maxFailures;
            _window = TimeSpan.FromMinutes(windowMinutes);
            _now = now ?? (() => DateTime.UtcNow);
        }

        public bool IsLockedOut(string username)
        {
            if (!_failuresByUsername.TryGetValue(Key(username), out var queue))
                return false;

            lock (queue)
            {
                Prune(queue);
                return queue.Count >= _maxFailures;
            }
        }

        public void RecordFailure(string username)
        {
            var queue = _failuresByUsername.GetOrAdd(Key(username), _ => new ConcurrentQueue<DateTime>());
            lock (queue)
            {
                Prune(queue);
                queue.Enqueue(_now());
            }
        }

        public void Reset(string username)
        {
            _failuresByUsername.TryRemove(Key(username), out _);
        }

        private void Prune(ConcurrentQueue<DateTime> queue)
        {
            var now = _now();
            while (queue.TryPeek(out var oldest) && now - oldest > _window)
                queue.TryDequeue(out _);
        }

        private static string Key(string username) => (username ?? "").Trim().ToLowerInvariant();
    }
}
