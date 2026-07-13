using System.Collections.Concurrent;

namespace SportsReservationAPI.Services
{
    // Registered as a singleton (Program.cs) — state must persist across requests
    // within the process. In-memory only: counters reset on backend restart,
    // which is acceptable since a single backend container runs in prod.
    public class PasswordResetRateLimiter
    {
        private readonly TimeSpan _emailCooldown;
        private readonly int _maxRequestsPerIpPerHour;
        private readonly int _maxGlobalPerDay;
        private readonly Func<DateTime> _now;

        private readonly ConcurrentDictionary<string, DateTime> _lastRequestByEmail = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestsByIp = new();
        private readonly ConcurrentQueue<DateTime> _globalRequests = new();

        public PasswordResetRateLimiter(
            int emailCooldownMinutes = 15,
            int maxRequestsPerIpPerHour = 5,
            int maxGlobalPerDay = 20,
            Func<DateTime>? now = null)
        {
            _emailCooldown = TimeSpan.FromMinutes(emailCooldownMinutes);
            _maxRequestsPerIpPerHour = maxRequestsPerIpPerHour;
            _maxGlobalPerDay = maxGlobalPerDay;
            _now = now ?? (() => DateTime.UtcNow);
        }

        public bool TryRegisterIpRequest(string ipKey)
        {
            var now = _now();
            var queue = _requestsByIp.GetOrAdd(ipKey, _ => new ConcurrentQueue<DateTime>());

            lock (queue)
            {
                while (queue.TryPeek(out var oldest) && now - oldest > TimeSpan.FromHours(1))
                    queue.TryDequeue(out _);

                if (queue.Count >= _maxRequestsPerIpPerHour)
                    return false;

                queue.Enqueue(now);
                return true;
            }
        }

        public bool TryRegisterGlobalRequest()
        {
            var now = _now();

            lock (_globalRequests)
            {
                while (_globalRequests.TryPeek(out var oldest) && now - oldest > TimeSpan.FromHours(24))
                    _globalRequests.TryDequeue(out _);

                if (_globalRequests.Count >= _maxGlobalPerDay)
                    return false;

                _globalRequests.Enqueue(now);
                return true;
            }
        }

        public bool IsEmailInCooldown(string email)
        {
            var now = _now();
            return _lastRequestByEmail.TryGetValue(email, out var last) && now - last < _emailCooldown;
        }

        public void RecordEmailRequest(string email)
        {
            _lastRequestByEmail[email] = _now();
        }
    }
}
