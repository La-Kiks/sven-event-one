using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Services;
using SportsReservationAPI.Configuration;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
{
    throw new Exception("FRONTEND_BASE_URL is not configured");
}

var frontendBaseUrl = apiSettings?.FrontendBaseUrl;

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ReservationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReservationDatabase")));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTeamDtoValidator>();

builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<StripeService>();

// CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "FrontendPolicy",
        policy =>
        {
            policy.WithOrigins(frontendBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
}
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
