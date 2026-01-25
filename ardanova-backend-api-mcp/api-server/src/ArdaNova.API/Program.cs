using System.Text.Json.Serialization;
using ArdaNova.API.Middleware;
using ArdaNova.API.EventBus.Extensions;
using ArdaNova.API.WebSocket.Extensions;
using ArdaNova.Application;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Storage services (S3, Local, etc.)
builder.Services.AddStorageServices(builder.Configuration);

// Add EventBus and WebSocket services
builder.Services.AddEventBus();
builder.Services.AddArdaNovaWebSocket();

// Add CORS for SignalR (allows Next.js server to connect)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalR", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "http://localhost:3000",
            "https://localhost:3000"
        };

        // Add configured origin if present
        var nextJsUrl = Environment.GetEnvironmentVariable("NEXTJS_URL");
        if (!string.IsNullOrEmpty(nextJsUrl))
        {
            allowedOrigins.Add(nextJsUrl);
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ArdaNova API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Description = "API Key authentication using the X-Api-Key header",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "ApiKey" },
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS for SignalR
app.UseCors("SignalR");

app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapArdaNovaHubs();

// Health check endpoint (no authentication required)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "ardanova-api"
})).AllowAnonymous();

app.Run();
