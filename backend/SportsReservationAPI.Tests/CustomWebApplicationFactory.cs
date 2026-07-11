using Microsoft.AspNetCore.Mvc.Testing;

namespace SportsReservationAPI.Tests;

// Boots the real app (Program.cs) as-is. The test container's own env vars
// (RESERVATION_DB_SERVER=test-database, etc. — see docker-compose.yml's
// `tests` service) already point Program.cs at the dedicated test database,
// so no service overrides are needed here. Program.cs's own startup logic
// (migrate, then dev-admin-seed) runs unmodified against that database.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
}
