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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
