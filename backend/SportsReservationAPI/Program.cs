using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SportsReservationAPI.Configuration;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Models.User;
using SportsReservationAPI.Services;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Loading ENV variables using Configuration/EnvLoader.cs
builder.Configuration.LoadToConfiguration();
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiKeys"));
builder.Services.AddOptions<ApiSettings>()
    .Bind(builder.Configuration.GetSection("ApiKeys"))
    .Validate(x =>
    !string.IsNullOrWhiteSpace(x.Stripe.WebhookSecret),
    "Stripe Webhok Secret is Missing"
    )
    .ValidateOnStart();

var apiSettings = builder.Configuration.GetSection("ApiKeys").Get<ApiSettings>();

if (string.IsNullOrWhiteSpace(apiSettings?.FrontendBaseUrl))
    throw new Exception("FRONTEND_BASE_URL is not configured");

var frontendBaseUrl = apiSettings?.FrontendBaseUrl;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme { Reference = new OpenApiReference {
            Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        new string[] {}
    }});
});

// Database
var db = builder.Configuration
    .GetSection("ConnectionStrings:ReservationDatabase")
    .Get<DbSettings>();

if (string.IsNullOrWhiteSpace(db!.Server))
    throw new Exception("RESERVATION_DB_SERVER is not configured.");
if (string.IsNullOrWhiteSpace(db.Database))
    throw new Exception("RESERVATION_DB_NAME is not configured.");

string connectionString = !string.IsNullOrWhiteSpace(db.User) && !string.IsNullOrWhiteSpace(db.Password)
    ? $"Server={db.Server};Database={db.Database};User Id={db.User};Password={db.Password};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true;"
    : $"Server={db.Server};Database={db.Database};Trusted_Connection=True;MultipleActiveResultSets=true;";

builder.Services.AddDbContext<ReservationContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTeamDtoValidator>();

// Services
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddHttpClient<MailService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "FrontendPolicy",
        policy =>
        {
            policy.WithOrigins(frontendBaseUrl!)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .WithExposedHeaders("Authorization");
           
        });
});

var app = builder.Build();

// Apply pending EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReservationContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Database migration failed");
        Console.WriteLine(ex);
        throw;
    }
}

// Dev-only: auto-create the admin account so a fresh local/dev database is
// immediately usable via `docker compose up --build`, with no manual step.
// Production already has its admin seeded out of band and is unaffected
// (ENVIRONMENT will not be "Development" there).
if (apiSettings?.Environment == "Development")
{
    using var seedScope = app.Services.CreateScope();
    var seedContext = seedScope.ServiceProvider.GetRequiredService<ReservationContext>();

    var adminUsername = app.Configuration["ADMIN_USERNAME"];
    var adminPassword = app.Configuration["ADMIN_PASSWORD"];

    if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
    {
        Console.WriteLine("ADMIN_USERNAME/ADMIN_PASSWORD not set - skipping dev admin seed");
    }
    else if (!seedContext.Users.Any(u => u.Username == adminUsername))
    {
        seedContext.Users.Add(new User
        {
            Username = adminUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin"
        });
        seedContext.SaveChanges();
        Console.WriteLine($"Dev admin account seeded: {adminUsername}");
    }
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
} else
{
    app.UseHttpsRedirection();

}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();