using System.Collections.Concurrent;

namespace SportsReservationAPI.Services
{
    // Registered as a singleton (Program.cs). Counts login attempts per
    // username in a sliding window (an attempt is counted when it starts, a
    // successful login resets the counter); once maxFailures is reached the
    // username is locked out (even for a correct password) until attempts age out.
    // Per-username rather than per-IP: the test suite logs in many times from
    // one address, and it protects the admin account regardless of how many
    // IPs an attacker uses. In-memory only, like PasswordResetRateLimiter.
    public class LoginRateLimiter
    {
        private readonly int _maxFailures;
        private readonly TimeSpan _window;
        private const int SweepEvery = 256;
        private readonly Func<DateTime> _now;
        private int _calls;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _failuresByUsername = new();

        public LoginRateLimiter(int maxFailures = 10, int windowMinutes = 15, Func<DateTime>? now = null)
        {
            _maxFailures = maxFailures;
            _window = TimeSpan.FromMinutes(windowMinutes);
            _now = now ?? (() => DateTime.UtcNow);
        }

        public int TrackedUsernameCount => _failuresByUsername.Count;

        // Atomically checks the lockout AND counts this attempt (under the
        // per-username lock), so a parallel burst can't slip extra guesses past
        // a check-then-record gap. Returns false when the username is locked out.
        // Success is signalled later via Reset. Unknown usernames are counted
        // exactly like known ones (no username enumeration).
        public bool TryBeginAttempt(string username)
        {
            if (Interlocked.Increment(ref _calls) % SweepEvery == 0)
                Sweep();

            var queue = _failuresByUsername.GetOrAdd(Key(username), _ => new ConcurrentQueue<DateTime>());
            lock (queue)
            {
                Prune(queue);
                if (queue.Count >= _maxFailures)
                    return false;
                queue.Enqueue(_now());
                return true;
            }
        }

        // Clears under the queue lock instead of removing the key, so a
        // concurrent TryBeginAttempt can't be orphaned by Reset. (Sweep can
        // still, rarely, drop a queue a racing caller already fetched; the cost
        // is one uncounted attempt, which is acceptable.)
        public void Reset(string username)
        {
            if (_failuresByUsername.TryGetValue(Key(username), out var queue))
            {
                lock (queue)
                    queue.Clear();
            }
        }

        // Drops usernames whose attempts have all aged out, bounding memory
        // against spraying of random usernames.
        private void Sweep()
        {
            foreach (var pair in _failuresByUsername)
            {
                lock (pair.Value)
                {
                    Prune(pair.Value);
                    if (pair.Value.IsEmpty)
                        _failuresByUsername.TryRemove(pair);
                }
            }
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
