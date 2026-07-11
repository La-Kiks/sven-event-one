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
