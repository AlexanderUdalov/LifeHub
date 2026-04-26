using LifeHub.Models;
using LifeHub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using OpenAI;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>(optional: true);

var dbPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "database", "lifehub.db")
);
builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
    options.LogTo(Console.WriteLine);
});

var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtConfig["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var aiApiKey = builder.Configuration["Ai:ApiKey"] ?? "";
var aiModel = builder.Configuration["Ai:Model"] ?? "gpt-5.4-mini";

if (!string.IsNullOrWhiteSpace(aiApiKey))
{
    builder.Services.AddChatClient(
        new OpenAIClient(aiApiKey).GetChatClient(aiModel).AsIChatClient());
}

builder.Services.AddScoped<ReflectionContextService>();
builder.Services.AddScoped<AiChatService>();
builder.Services.AddScoped<AddictionTriggerGuidanceService>();

builder.Services
    .AddAuthorization()
    .AddOpenApi()
    .AddCors(options =>
{
    options.AddPolicy("LifeHubCors", policy =>
    {
        policy.WithOrigins("http://tauri.localhost")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
})
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    });

var app = builder.Build();

app.UseCors("LifeHubCors");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
