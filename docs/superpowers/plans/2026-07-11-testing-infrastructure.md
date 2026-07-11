# Manual Testing Workflow + Automated API Test Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the user a documented manual-testing checklist plus a dev-only sample-data seeder for exploring the app in the browser, and an xUnit integration test suite (run via a dedicated Docker Compose service) that exercises the real API against a real, ephemeral SQL Server database.

**Architecture:** A new `backend/SportsReservationAPI.Tests` xUnit project uses `WebApplicationFactory<Program>` to boot the actual app in-process and send it real HTTP requests, pointed at a brand-new `test-database` Compose service (separate from the dev database, so automated runs never touch manually-created data). A shared `ApiTestFixture` wipes the test database via a direct SQL connection *before* the app boots (so `Program.cs`'s own migration + dev-admin-seed logic runs against a guaranteed-clean slate every time — including when the suite is re-run without recreating the containers), then all test classes share one xUnit collection so they execute sequentially against that single prepared instance.

**Tech Stack:** xUnit, `Microsoft.AspNetCore.Mvc.Testing`, EF Core 8 / SQL Server, Docker Compose.

## Global Constraints

- Everything runs via Docker Compose — no bare `dotnet`/`npm`/`ng` commands, for the app or for the tests (`docker compose run --rm tests` is how the suite runs).
- The test database is separate from the dev database (`RESERVATION_DB_NAME`/`ADMIN_*` differ) so automated test runs never depend on or corrupt manually-created dev data.
- Every step's verification uses `docker compose` commands and shows the exact expected output — there is no `dotnet test`/`npm test` available outside a container.

---

### Task 1: Manual testing guide

**Files:**
- Create: `docs/manual-testing-guide.md`
- Modify: `README.md` (add a link under "Development notes")

**Interfaces:** None — documentation only, no code dependencies on other tasks.

- [ ] **Step 1: Write the guide**

Create `docs/manual-testing-guide.md`:

```markdown
# Manual testing guide

Walks through the full participant lifecycle against the running dev stack (`docker compose up --build`). Assumes `.env` has `ENVIRONMENT=Development` and `ADMIN_USERNAME`/`ADMIN_PASSWORD` set (the admin account is auto-seeded on startup — see `CLAUDE.md`).

## 1. Register a team

1. Open `http://localhost:<UI_PORT>/inscription` and fill in the 3-step form for two players.
2. Submit. Expect a success modal mentioning an activation email.

## 2. Find the activation link

If `MAILGUN_API_KEY`/`MAILGUN_DOMAIN` are set in `.env` and the participant's email is an authorized recipient on your Mailgun sandbox, check that inbox directly and skip to step 3.

Otherwise, fetch the token from the database:

```bash
MSYS_NO_PATHCONV=1 docker exec sports-reservation-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '<your DB_PASSWORD from .env>' -d SportsReservationDB -C \
  -Q "SELECT Username, VerificationToken FROM Users WHERE VerificationToken IS NOT NULL"
```

Build the URL yourself: `http://localhost:<UI_PORT>/activer-compte?token=<VerificationToken>`.

## 3. Activate

1. Open the URL from step 2.
2. Set a password (8+ characters) and submit.
3. Expect a redirect to `/mon-equipe` showing the team you just registered.

## 4. Edit your team

1. Change a field (e.g. team name, a player's category).
2. Save. Expect a success message and the change to persist on refresh.
3. Confirm the payment status badge is present but not clickable (participants can't toggle it).

## 5. Check the admin view

1. Log out (top bar).
2. Log in at `/login` with your `ADMIN_USERNAME`/`ADMIN_PASSWORD`.
3. Expect a redirect to `/teams` (not `/mon-equipe`).
4. Find the team from step 1, open its detail panel, confirm your step 4 edit is reflected and the account shows as "Activé".
5. Toggle the payment badge, confirm it updates.

## 6. Confirm role gating

1. While still logged in as admin, navigate directly to `/mon-equipe`. Expect a redirect back to `/teams` (not `/login` — wrong role, not logged out).
2. Log out, log back in with the participant credentials from step 3.
3. Navigate directly to `/teams`. Expect a redirect back to `/mon-equipe`.
```

- [ ] **Step 2: Link it from the README**

In `README.md`, find:

```markdown
## Development notes

See [`CLAUDE.md`](CLAUDE.md) for a deeper architecture walkthrough (auth/role model, EF Core migration conventions, frontend runtime config injection, etc.). There is no automated test suite in this repo — changes are verified by rebuilding via `docker compose up --build` and exercising the running app.
```

Replace with:

```markdown
## Development notes

See [`CLAUDE.md`](CLAUDE.md) for a deeper architecture walkthrough (auth/role model, EF Core migration conventions, frontend runtime config injection, etc.). See [`docs/manual-testing-guide.md`](docs/manual-testing-guide.md) to manually exercise the participant/admin flows in a browser, and the "Automated tests" section below to run the API test suite.

### Automated tests

```bash
docker compose run --rm tests
```

Runs the xUnit integration test suite (`backend/SportsReservationAPI.Tests`) against a dedicated, ephemeral SQL Server instance — never your dev database. See `CLAUDE.md` for how the suite is structured.
```

- [ ] **Step 3: Commit**

```bash
git add docs/manual-testing-guide.md README.md
git commit -m "docs: add manual testing guide"
```

---

### Task 2: Dev-only sample-data seeder

**Files:**
- Create: `backend/SportsReservationAPI/Controllers/DevController.cs`

**Interfaces:**
- Consumes: `TeamService.CreateTeamWithPlayersAsync(CreateTeamDto, List<CreatePlayerDto>)` (existing, returns `Task<int>` team id), `ApiSettings.Environment` (existing, via `IOptions<ApiSettings>`).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Add the controller**

Create `backend/SportsReservationAPI/Controllers/DevController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    // Dev-only: populates the dev database with sample teams so the admin
    // panel isn't empty while exploring the app manually. Inert in production
    // (same ApiSettings.Environment gate as the dev admin seed in Program.cs).
    [Route("api/dev")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly TeamService _teamService;
        private readonly ApiSettings _apiSettings;

        public DevController(TeamService teamService, IOptions<ApiSettings> apiSettings)
        {
            _teamService = teamService;
            _apiSettings = apiSettings.Value;
        }

        [HttpPost("seed-sample-data")]
        public async Task<IActionResult> SeedSampleData()
        {
            if (_apiSettings.Environment != "Development")
                return NotFound();

            var samples = new[]
            {
                (Team: "Les Foudres", Version: "short", Admin: "nationale", P1Cat: "man", P2Cat: "woman"),
                (Team: "Team Alpha", Version: "long", Admin: "gendarmerie", P1Cat: "man", P2Cat: "man"),
                (Team: "Les Panthères", Version: "short", Admin: "municipale", P1Cat: "woman", P2Cat: "woman"),
                (Team: "Escouade 54", Version: "long", Admin: "pompier", P1Cat: "man", P2Cat: "woman"),
                (Team: "Team Bravo", Version: "short", Admin: "militaire", P1Cat: "woman", P2Cat: "man"),
            };

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var created = new List<object>();

            foreach (var sample in samples)
            {
                var teamDto = new CreateTeamDto
                {
                    TeamName = sample.Team,
                    Version = sample.Version,
                    Administration = sample.Admin
                };
                var playerDtos = new List<CreatePlayerDto>
                {
                    new()
                    {
                        FirstName = "Alice", LastName = "Dupont",
                        Email = $"alice.{suffix}.{created.Count}@example.com",
                        PhoneNumber = "+33612345678", Category = sample.P1Cat,
                        Outfit = "yes", Volunteer = false, AcceptMails = true
                    },
                    new()
                    {
                        FirstName = "Bob", LastName = "Martin",
                        Email = $"bob.{suffix}.{created.Count}@example.com",
                        PhoneNumber = "+33612345679", Category = sample.P2Cat,
                        Outfit = "no", Volunteer = true, AcceptMails = true
                    }
                };

                var teamId = await _teamService.CreateTeamWithPlayersAsync(teamDto, playerDtos);
                created.Add(new { teamId, name = sample.Team });
            }

            return Ok(new { Message = $"{created.Count} sample teams created.", Teams = created });
        }
    }
}
```

- [ ] **Step 2: Rebuild and verify it works in dev**

```bash
docker compose up --build -d
curl -s -X POST "http://localhost:7163/api/dev/seed-sample-data"
```

Expected: a JSON body with `"Message": "5 sample teams created."` and a `Teams` array of 5 `{teamId, name}` entries.

- [ ] **Step 3: Verify it's a 404 outside development**

This can't be checked against the running dev stack (it always has `ENVIRONMENT=Development`) — instead, read the guard directly: confirm `if (_apiSettings.Environment != "Development") return NotFound();` is present in the file (Step 1). This same gate is exercised for real by Task 3's integration tests, which run with `ENVIRONMENT=Development` unset in one dedicated test case (see Task 4).

- [ ] **Step 4: Clean up the sample teams from your dev database**

```bash
MSYS_NO_PATHCONV=1 docker exec sports-reservation-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '<your DB_PASSWORD from .env>' -d SportsReservationDB -C \
  -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM Users WHERE TeamId IN (SELECT Id FROM Teams WHERE Name IN ('Les Foudres','Team Alpha','Les Panthères','Escouade 54','Team Bravo')); DELETE FROM Players WHERE TeamId IN (SELECT Id FROM Teams WHERE Name IN ('Les Foudres','Team Alpha','Les Panthères','Escouade 54','Team Bravo')); DELETE FROM Teams WHERE Name IN ('Les Foudres','Team Alpha','Les Panthères','Escouade 54','Team Bravo');"
```

(Or leave them — they're harmless sample data. This step is only needed if you want a clean dev database again.)

- [ ] **Step 5: Commit**

```bash
git add backend/SportsReservationAPI/Controllers/DevController.cs
git commit -m "feat: add dev-only sample data seeder endpoint"
```

---

### Task 3: Test project scaffolding

**Files:**
- Create: `backend/SportsReservationAPI.Tests/SportsReservationAPI.Tests.csproj`
- Create: `backend/SportsReservationAPI.Tests/Dockerfile`
- Create: `backend/SportsReservationAPI.Tests/CustomWebApplicationFactory.cs`
- Create: `backend/SportsReservationAPI.Tests/ApiTestFixture.cs`
- Create: `backend/SportsReservationAPI.Tests/SmokeTests.cs`
- Modify: `backend/SportsReservationAPI/Program.cs` (append one line)
- Modify: `backend/SportsReservationAPI/SportsReservationAPI.sln` (register the new project)
- Modify: `docker-compose.yml` (add `test-database` and `tests` services)

**Interfaces:**
- Produces: `CustomWebApplicationFactory` (empty `WebApplicationFactory<Program>` subclass — the container's own env vars already route `Program.cs` at the test database, so no `ConfigureWebHost` override is needed). `ApiTestFixture` with `HttpClient Client`, `ReservationContext CreateDbContext()`, `Task<(int TeamId, string Jwt)> RegisterAndActivateTeamAsync(string teamName, string participant1Email, string participant2Email, string password = "TestPassword123")`, `Task<string> GetAdminJwtAsync()`, `static string UniqueEmail(string label)`. The `[Collection("Api")]` attribute (defined via `ApiCollection`) that every later test class must carry so all tests share one fixture instance and run sequentially.

- [ ] **Step 1: Make `Program` accessible to the test assembly**

`Program.cs` uses top-level statements, which generate an `internal` `Program` class — `WebApplicationFactory<Program>` from a separate assembly needs it `public`. Add this as the very last line of `backend/SportsReservationAPI/Program.cs` (after `app.Run();`):

```csharp
public partial class Program { }
```

- [ ] **Step 2: Create the test project file**

Create `backend/SportsReservationAPI.Tests/SportsReservationAPI.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.10" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SportsReservationAPI\SportsReservationAPI.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Register the project in the solution**

Open `backend/SportsReservationAPI/SportsReservationAPI.sln`. It currently reads:

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.14.36811.4
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SportsReservationAPI", "SportsReservationAPI.csproj", "{2955F691-6D69-4A21-A900-B811A2319AF4}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {4B8EDBFC-03D9-4DCA-9607-3C98B1D44F8B}
	EndGlobalSection
EndGlobal
```

Replace its entire contents with (new project added with a fixed GUID `{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}` — any valid GUID works, this one just needs to be unique within the file):

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.14.36811.4
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SportsReservationAPI", "SportsReservationAPI.csproj", "{2955F691-6D69-4A21-A900-B811A2319AF4}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SportsReservationAPI.Tests", "..\SportsReservationAPI.Tests\SportsReservationAPI.Tests.csproj", "{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{2955F691-6D69-4A21-A900-B811A2319AF4}.Release|Any CPU.Build.0 = Release|Any CPU
		{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {4B8EDBFC-03D9-4DCA-9607-3C98B1D44F8B}
	EndGlobalSection
EndGlobal
```

- [ ] **Step 4: Add the test project's Dockerfile**

Create `backend/SportsReservationAPI.Tests/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

COPY ["SportsReservationAPI.Tests/SportsReservationAPI.Tests.csproj", "SportsReservationAPI.Tests/"]
COPY ["SportsReservationAPI/SportsReservationAPI.csproj", "SportsReservationAPI/"]
RUN dotnet restore "SportsReservationAPI.Tests/SportsReservationAPI.Tests.csproj"

COPY . .

WORKDIR /src/SportsReservationAPI.Tests
ENTRYPOINT ["dotnet", "test", "--logger", "console;verbosity=normal"]
```

- [ ] **Step 5: Add the `CustomWebApplicationFactory`**

Create `backend/SportsReservationAPI.Tests/CustomWebApplicationFactory.cs`:

```csharp
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
```

- [ ] **Step 6: Add the shared test fixture**

Create `backend/SportsReservationAPI.Tests/ApiTestFixture.cs`:

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsReservationAPI.Models;
using Xunit;

namespace SportsReservationAPI.Tests;

public class ApiTestFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Drop the test database (if it exists) BEFORE the app boots, so that
        // when Factory.CreateClient() triggers Program.cs's own startup logic
        // (migrate, then dev-admin-seed), it runs against a guaranteed-clean
        // slate every single time this fixture is created — including when
        // the whole suite is re-run without recreating the containers.
        await DropTestDatabaseAsync();

        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }

    public ReservationContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ReservationContext>();
    }

    // Registers a team via the real public endpoint, fetches the resulting
    // verification token directly from the DB (bypassing email — Mailgun
    // isn't configured for the test environment), activates the account,
    // and returns the team id plus the participant's JWT.
    public async Task<(int TeamId, string Jwt)> RegisterAndActivateTeamAsync(
        string teamName, string participant1Email, string participant2Email, string password = "TestPassword123")
    {
        var createResponse = await Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName, version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1Email, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = participant2Email, phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var teamId = created.GetProperty("teamId").GetInt32();

        using var context = CreateDbContext();
        var user = await context.Users.FirstAsync(u => u.Username == participant1Email);
        var token = user.VerificationToken!;

        var activateResponse = await Client.PostAsJsonAsync("/api/auth/activate", new { token, password });
        activateResponse.EnsureSuccessStatusCode();
        var activated = await activateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = activated.GetProperty("token").GetString()!;

        return (teamId, jwt);
    }

    public async Task<string> GetAdminJwtAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = Environment.GetEnvironmentVariable("ADMIN_USERNAME"),
            password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    public static string UniqueEmail(string label) => $"{label}-{Guid.NewGuid():N}@example.com";

    private static async Task DropTestDatabaseAsync()
    {
        var dbName = Environment.GetEnvironmentVariable("RESERVATION_DB_NAME") ?? "SportsReservationTestDB";
        var server = Environment.GetEnvironmentVariable("RESERVATION_DB_SERVER") ?? "test-database,1433";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

        var masterConnectionString =
            $"Server={server};Database=master;User Id={user};Password={password};Encrypt=False;TrustServerCertificate=True;";

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID('{dbName}') IS NOT NULL BEGIN " +
            $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"DROP DATABASE [{dbName}]; END";
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiTestFixture>
{
}
```

- [ ] **Step 7: Add a smoke test**

Create `backend/SportsReservationAPI.Tests/SmokeTests.cs`:

```csharp
using System.Net;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class SmokeTests
{
    private readonly ApiTestFixture _fixture;

    public SmokeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTeamCount_ReturnsOkWithZeroTeamsOnFreshDatabase()
    {
        var response = await _fixture.Client.GetAsync("/api/teams/count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"current\":0", body);
        Assert.Contains("\"isFull\":false", body);
    }
}
```

- [ ] **Step 8: Add the Compose services**

In `docker-compose.yml`, find the `database:` service block and its trailing `volumes:` section:

```yaml
  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sports-reservation-db
    ports:
      - "${DB_PORT}:1433"
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "${DB_PASSWORD}"
      MSSQL_PID: Express
    volumes:
      - sql-data:/var/opt/mssql
    healthcheck:
      test:
        [
          "CMD-SHELL",
          "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -Q 'SELECT 1' -No || exit 1",
        ]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s # give SQL Server time to initialize

volumes:
  sql-data:
```

Replace with (adds `test-database` and `tests` after `database`, keeps the `volumes:` section unchanged since `test-database` deliberately has no persisted volume):

```yaml
  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sports-reservation-db
    ports:
      - "${DB_PORT}:1433"
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "${DB_PASSWORD}"
      MSSQL_PID: Express
    volumes:
      - sql-data:/var/opt/mssql
    healthcheck:
      test:
        [
          "CMD-SHELL",
          "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -Q 'SELECT 1' -No || exit 1",
        ]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s # give SQL Server time to initialize

  test-database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sports-reservation-test-db
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "Test_Password123!"
      MSSQL_PID: Express
    healthcheck:
      test:
        [
          "CMD-SHELL",
          "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Test_Password123!' -Q 'SELECT 1' -No || exit 1",
        ]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    # No volume — ephemeral on purpose, so every run can start from a known state.

  tests:
    build:
      context: ./backend
      dockerfile: SportsReservationAPI.Tests/Dockerfile
    container_name: sports-reservation-tests
    environment:
      ENVIRONMENT: Development
      RESERVATION_DB_SERVER: test-database,1433
      RESERVATION_DB_NAME: SportsReservationTestDB
      DB_USER: sa
      DB_PASSWORD: Test_Password123!
      JWT_KEY: test-only-signing-key-not-for-production-use
      JWT_ISSUER: SportsReservationAPI
      ADMIN_USERNAME: testadmin
      ADMIN_PASSWORD: TestAdminPassword123!
      API_BASE_URL: http://localhost:8080
      FRONTEND_BASE_URL: http://localhost:4200
      STRIPE_WEBHOOK_SECRET: whsec_test_dummy_value
    depends_on:
      test-database:
        condition: service_healthy

volumes:
  sql-data:
```

- [ ] **Step 9: Run the suite for the first time**

```bash
docker compose run --rm tests
```

Expected: Docker builds the `tests` image, waits for `test-database` to become healthy, then output ending with something like:

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

If it fails with a connection error, wait a few seconds and retry — `test-database`'s first boot can take longer than its healthcheck's `start_period` accounts for.

- [ ] **Step 10: Verify re-running without teardown still passes**

```bash
docker compose run --rm tests
```

Expected: same `Passed! ... Total: 1` output — proves `DropTestDatabaseAsync` correctly resets state even when `test-database`'s container was left running from Step 9.

- [ ] **Step 11: Commit**

```bash
git add backend/SportsReservationAPI/Program.cs backend/SportsReservationAPI/SportsReservationAPI.sln \
  backend/SportsReservationAPI.Tests docker-compose.yml
git commit -m "test: scaffold xUnit integration test project against ephemeral SQL Server"
```

---

### Task 4: Auth endpoint tests

**Files:**
- Create: `backend/SportsReservationAPI.Tests/AuthTests.cs`

**Interfaces:**
- Consumes: `ApiTestFixture.Client`, `ApiTestFixture.CreateDbContext()`, `ApiTestFixture.RegisterAndActivateTeamAsync(...)`, `ApiTestFixture.UniqueEmail(string)` (all from Task 3).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the tests**

Create `backend/SportsReservationAPI.Tests/AuthTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class AuthTests
{
    private readonly ApiTestFixture _fixture;

    public AuthTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithCorrectAdminCredentials_ReturnsAdminRoleToken()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = Environment.GetEnvironmentVariable("ADMIN_USERNAME"),
            password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Admin", body.GetProperty("role").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = Environment.GetEnvironmentVariable("ADMIN_USERNAME"),
            password = "definitely-not-the-right-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForPendingUnactivatedAccount_ReturnsUnauthorizedNotServerError()
    {
        var email = ApiTestFixture.UniqueEmail("pending");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "PendingLoginTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("pending2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        // Account exists but has never been activated (empty PasswordHash) — must 401 cleanly, not 500.
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WithValidToken_SetsPasswordAndReturnsUserRoleToken()
    {
        var participant1 = ApiTestFixture.UniqueEmail("activate-ok");
        var teamResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ActivateOkTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("activate-ok2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        teamResponse.EnsureSuccessStatusCode();

        using var context = _fixture.CreateDbContext();
        var user = await context.Users.FirstAsync(u => u.Username == participant1);
        var token = user.VerificationToken!;

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token, password = "BrandNewPassword123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("User", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Activate_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token = "this-token-does-not-exist", password = "SomePassword123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WithExpiredToken_ReturnsBadRequest()
    {
        var participant1 = ApiTestFixture.UniqueEmail("expired");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ExpiredTokenTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("expired2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        // No API path produces an already-expired token — back-date it directly
        // to exercise UserService.VerifyAndSetPasswordAsync's expiry check.
        string token;
        using (var context = _fixture.CreateDbContext())
        {
            var user = await context.Users.FirstAsync(u => u.Username == participant1);
            token = user.VerificationToken!;
            user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(-1);
            await context.SaveChangesAsync();
        }

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token, password = "SomePassword123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WithPasswordUnder8Characters_ReturnsBadRequest()
    {
        var participant1 = ApiTestFixture.UniqueEmail("short-pw");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ShortPwTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("short-pw2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        using var context = _fixture.CreateDbContext();
        var user = await context.Users.FirstAsync(u => u.Username == participant1);

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token = user.VerificationToken, password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the suite**

```bash
docker compose run --rm tests
```

Expected: `Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8` (the smoke test plus these 7).

- [ ] **Step 3: Commit**

```bash
git add backend/SportsReservationAPI.Tests/AuthTests.cs
git commit -m "test: add auth endpoint integration tests"
```

---

### Task 5: Team registration tests

**Files:**
- Create: `backend/SportsReservationAPI.Tests/TeamRegistrationTests.cs`

**Interfaces:**
- Consumes: same fixture members as Task 4.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the tests**

Create `backend/SportsReservationAPI.Tests/TeamRegistrationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class TeamRegistrationTests
{
    private readonly ApiTestFixture _fixture;

    public TeamRegistrationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateTeam_WithValidPayload_ReturnsTeamId()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ValidRegTeam", version = "long", administration = "pompier" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("reg1"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("reg2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("teamId").GetInt32() > 0);
    }

    [Fact]
    public async Task CreateTeam_WithOnePlayer_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "OnePlayerTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("onlyone"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_WithDuplicateParticipant1Email_ReturnsBadRequest()
    {
        var sharedEmail = ApiTestFixture.UniqueEmail("dup");
        var payload = new
        {
            teamDto = new { teamName = "FirstDupTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = sharedEmail, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("dup2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        };
        var firstResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", payload);
        firstResponse.EnsureSuccessStatusCode();

        var secondPayload = new
        {
            teamDto = new { teamName = "SecondDupTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Someone", lastName = "Else", email = sharedEmail, phoneNumber = "+33612345680", category = "man", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Other", lastName = "Person", email = ApiTestFixture.UniqueEmail("dup3"), phoneNumber = "+33612345681", category = "woman", outfit = "no", volunteer = false, acceptMails = true }
            }
        };

        var secondResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", secondPayload);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("associé", body);
    }

    [Fact]
    public async Task GetTeamCount_IncreasesAfterRegistration()
    {
        var before = await _fixture.Client.GetFromJsonAsync<JsonElement>("/api/teams/count");
        var beforeCount = before.GetProperty("current").GetInt32();

        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "CountTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("count1"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("count2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var after = await _fixture.Client.GetFromJsonAsync<JsonElement>("/api/teams/count");
        Assert.Equal(beforeCount + 1, after.GetProperty("current").GetInt32());
    }

    [Fact]
    public async Task CreateTeam_DerivesMixtCategoryWhenPlayersDiffer()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "MixtCategoryTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("mixt1"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("mixt2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var teamId = created.GetProperty("teamId").GetInt32();

        var adminJwt = await _fixture.GetAdminJwtAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Teams/{teamId}");
        request.Headers.Add("Authorization", $"Bearer {adminJwt}");
        var teamResponse = await _fixture.Client.SendAsync(request);
        var team = await teamResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("mixt", team.GetProperty("category").GetString());
    }
}
```

- [ ] **Step 2: Run the suite**

```bash
docker compose run --rm tests
```

Expected: `Total: 13` (8 from Tasks 3-4 plus these 5), all passed.

Note: the `MaxTeams` cap (52) is deliberately not tested end-to-end here — doing so would mean 52 real HTTP registrations on every single test run just to check one `>=` comparison. If `TeamsController.MaxTeams` ever becomes configurable, add a test that overrides it to a small number for a fast check; until then this one rule is left to manual/code review.

- [ ] **Step 3: Commit**

```bash
git add backend/SportsReservationAPI.Tests/TeamRegistrationTests.cs
git commit -m "test: add team registration integration tests"
```

---

### Task 6: My-team (participant self-service) tests

**Files:**
- Create: `backend/SportsReservationAPI.Tests/MyTeamTests.cs`

**Interfaces:**
- Consumes: `ApiTestFixture.RegisterAndActivateTeamAsync(...)` (Task 3) — the primary way these tests get an authenticated participant.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the tests**

Create `backend/SportsReservationAPI.Tests/MyTeamTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class MyTeamTests
{
    private readonly ApiTestFixture _fixture;

    public MyTeamTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpRequestMessage AuthedRequest(HttpMethod method, string url, string jwt)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {jwt}");
        return request;
    }

    [Fact]
    public async Task GetMyTeam_ReturnsOwnTeamWithBothPlayers()
    {
        var (teamId, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "GetMyTeamTest", ApiTestFixture.UniqueEmail("getmt1"), ApiTestFixture.UniqueEmail("getmt2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(teamId, body.GetProperty("id").GetInt32());
        Assert.Equal(2, body.GetProperty("players").GetArrayLength());
    }

    [Fact]
    public async Task GetMyTeam_WithAdminToken_ReturnsForbidden()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", adminJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTeam_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/teams/my-team");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyTeam_WithValidPayload_PersistsChangesAndLeavesIsPaidUntouched()
    {
        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "UpdateMyTeamTest", ApiTestFixture.UniqueEmail("updmt1"), ApiTestFixture.UniqueEmail("updmt2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var players = current.GetProperty("players").EnumerateArray().ToList();

        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "UpdateMyTeamTest - Renamed", version = "long", administration = "pompier" },
            playerDtos = new[]
            {
                new { id = players[0].GetProperty("id").GetInt32(), firstName = "AliceUpdated", lastName = "Test", email = players[0].GetProperty("email").GetString(), phoneNumber = "+33612345678", category = "woman", outfit = "yes", volunteer = true, acceptMails = true },
                new { id = players[1].GetProperty("id").GetInt32(), firstName = "BobUpdated", lastName = "Test", email = players[1].GetProperty("email").GetString(), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var updateResponse = await _fixture.Client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var after = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("UpdateMyTeamTest - Renamed", after.GetProperty("name").GetString());
        Assert.False(after.GetProperty("isPaid").GetBoolean());
        Assert.Equal("AliceUpdated", after.GetProperty("players")[0].GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task UpdateMyTeam_WithDuplicatePlayerId_ReturnsBadRequest()
    {
        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "DuplicateIdTeam", ApiTestFixture.UniqueEmail("dupid1"), ApiTestFixture.UniqueEmail("dupid2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var firstPlayerId = current.GetProperty("players")[0].GetProperty("id").GetInt32();

        // Both entries reference the SAME player id — this is the exact regression
        // this test guards against (found during this project's own code review).
        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "DuplicateIdTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { id = firstPlayerId, firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("dupid-a"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { id = firstPlayerId, firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("dupid-b"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var response = await _fixture.Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyTeam_ChangingParticipant1Email_UpdatesLoginUsername()
    {
        var oldEmail = ApiTestFixture.UniqueEmail("emailsync-old");
        var newEmail = ApiTestFixture.UniqueEmail("emailsync-new");
        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync("EmailSyncTeam", oldEmail, ApiTestFixture.UniqueEmail("emailsync2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var players = current.GetProperty("players").EnumerateArray().ToList();

        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "EmailSyncTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { id = players[0].GetProperty("id").GetInt32(), firstName = "Alice", lastName = "Test", email = newEmail, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { id = players[1].GetProperty("id").GetInt32(), firstName = "Bob", lastName = "Test", email = players[1].GetProperty("email").GetString(), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        (await _fixture.Client.SendAsync(updateRequest)).EnsureSuccessStatusCode();

        var oldLoginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = oldEmail, password = "TestPassword123" });
        var newLoginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = newEmail, password = "TestPassword123" });

        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateMyTeam_ChangingParticipant1EmailToAnotherAccountsEmail_ReturnsBadRequest()
    {
        var otherTeamEmail = ApiTestFixture.UniqueEmail("conflict-other");
        await _fixture.RegisterAndActivateTeamAsync("ConflictOtherTeam", otherTeamEmail, ApiTestFixture.UniqueEmail("conflict-other2"));

        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "ConflictMyTeam", ApiTestFixture.UniqueEmail("conflict-mine"), ApiTestFixture.UniqueEmail("conflict-mine2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var players = current.GetProperty("players").EnumerateArray().ToList();

        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "ConflictMyTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                // Attempts to steal the other team's participant-1 email as this team's own.
                new { id = players[0].GetProperty("id").GetInt32(), firstName = "Alice", lastName = "Test", email = otherTeamEmail, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { id = players[1].GetProperty("id").GetInt32(), firstName = "Bob", lastName = "Test", email = players[1].GetProperty("email").GetString(), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var response = await _fixture.Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the suite and confirm all pass**

```bash
docker compose run --rm tests
```

Expected: `Total: 20` (13 from Tasks 3-5 plus these 7), all passed.

- [ ] **Step 3: Prove the duplicate-id regression test actually catches the bug it's named for**

Temporarily revert the fix in `backend/SportsReservationAPI/Services/TeamService.cs`. Find:

```csharp
            // Submitted ids must be exactly this team's two player ids (no duplicates,
            // no ids belonging to another team) — matching by array position isn't safe.
            var submittedIds = playerDtos.Select(p => p.Id).ToHashSet();
            var existingIds = team.Players.Select(p => p.Id).ToHashSet();
            if (submittedIds.Count != playerDtos.Count || !submittedIds.SetEquals(existingIds))
                throw new ValidationException("Player ids must exactly match this team's existing players.");

            // Participant 1 is always the earliest-created player row (see CLAUDE.md) —
```

Temporarily replace with (removes the validation):

```csharp
            // Participant 1 is always the earliest-created player row (see CLAUDE.md) —
```

Run:

```bash
docker compose run --rm tests
```

Expected: `Failed:     1` — specifically `MyTeamTests.UpdateMyTeam_WithDuplicatePlayerId_ReturnsBadRequest` (the endpoint now returns 200 instead of 400 since nothing rejects the duplicate id).

Restore the fix (undo the edit — put the validation block back exactly as it was), then confirm green again:

```bash
docker compose run --rm tests
```

Expected: `Total: 20`, all passed again.

- [ ] **Step 4: Commit**

```bash
git add backend/SportsReservationAPI.Tests/MyTeamTests.cs
git commit -m "test: add my-team integration tests, including duplicate-id regression"
```

---

### Task 7: Admin endpoint tests

**Files:**
- Create: `backend/SportsReservationAPI.Tests/AdminTeamsTests.cs`

**Interfaces:**
- Consumes: `ApiTestFixture.GetAdminJwtAsync()`, `ApiTestFixture.RegisterAndActivateTeamAsync(...)` (Task 3).
- Produces: nothing — last task in this plan.

- [ ] **Step 1: Write the tests**

Create `backend/SportsReservationAPI.Tests/AdminTeamsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class AdminTeamsTests
{
    private readonly ApiTestFixture _fixture;

    public AdminTeamsTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpRequestMessage AuthedRequest(HttpMethod method, string url, string jwt)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {jwt}");
        return request;
    }

    [Fact]
    public async Task GetAllTeams_WithAdminToken_ReturnsOk()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Teams/teams", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTeams_WithParticipantToken_ReturnsForbidden()
    {
        var (_, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "NoAdminAccessTeam", ApiTestFixture.UniqueEmail("noadmin1"), ApiTestFixture.UniqueEmail("noadmin2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Teams/teams", participantJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTeams_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/Teams/teams");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePaymentStatus_TogglesIsPaid()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var (teamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "PaymentToggleTeam", ApiTestFixture.UniqueEmail("paytoggle1"), ApiTestFixture.UniqueEmail("paytoggle2"));

        var patchRequest = AuthedRequest(HttpMethod.Patch, $"/api/Teams/{teamId}/payment", adminJwt);
        patchRequest.Content = JsonContent.Create(new { isPaid = true });
        var patchResponse = await _fixture.Client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var getResponse = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/Teams/{teamId}", adminJwt));
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isPaid").GetBoolean());
    }

    [Fact]
    public async Task UpdatePaymentStatus_WithParticipantToken_ReturnsForbidden()
    {
        var (teamId, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "PaymentForbiddenTeam", ApiTestFixture.UniqueEmail("payforbid1"), ApiTestFixture.UniqueEmail("payforbid2"));

        var patchRequest = AuthedRequest(HttpMethod.Patch, $"/api/Teams/{teamId}/payment", participantJwt);
        patchRequest.Content = JsonContent.Create(new { isPaid = true });

        var response = await _fixture.Client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_RemovesTeamAndCascadesPlayersWithoutError()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var (teamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "DeleteMeTeam", ApiTestFixture.UniqueEmail("delete1"), ApiTestFixture.UniqueEmail("delete2"));

        var deleteResponse = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Delete, $"/api/Teams/{teamId}", adminJwt));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/Teams/{teamId}", adminJwt));
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_ForTeamWithNoAccount_ReturnsOk()
    {
        // Register via the API but don't go through RegisterAndActivateTeamAsync —
        // create-team already creates a pending account, so use create-account's
        // "resend" path (unverified account) rather than "no account at all",
        // which only occurs for pre-existing data this app can't produce via its
        // own API anymore. Resend is the realistic, reachable case to test here.
        var email1 = ApiTestFixture.UniqueEmail("resend1");
        var createResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ResendAccountTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = email1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("resend2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var teamId = created.GetProperty("teamId").GetInt32();

        var adminJwt = await _fixture.GetAdminJwtAsync();
        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/Teams/{teamId}/create-account", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_ForAlreadyActivatedTeam_ReturnsConflict()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var (teamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "AlreadyActiveTeam", ApiTestFixture.UniqueEmail("active1"), ApiTestFixture.UniqueEmail("active2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/Teams/{teamId}/create-account", adminJwt));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPlayers_WithAdminToken_ReturnsOk()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Players", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPlayers_WithParticipantToken_ReturnsForbidden()
    {
        var (_, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "PlayersForbiddenTeam", ApiTestFixture.UniqueEmail("playersforbid1"), ApiTestFixture.UniqueEmail("playersforbid2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Players", participantJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the full suite one last time**

```bash
docker compose run --rm tests
```

Expected: `Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30`.

- [ ] **Step 3: Commit**

```bash
git add backend/SportsReservationAPI.Tests/AdminTeamsTests.cs
git commit -m "test: add admin endpoint integration tests with role authorization checks"
```

