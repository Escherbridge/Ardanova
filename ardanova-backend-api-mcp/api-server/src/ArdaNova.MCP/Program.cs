using ArdaNova.Application;
using ArdaNova.Infrastructure;
using ArdaNova.MCP.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true);

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add MCP server with tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(UserTools).Assembly);

// Register tool types for DI
builder.Services.AddScoped<UserTools>();
builder.Services.AddScoped<ProjectTools>();
builder.Services.AddScoped<GuildTools>();
builder.Services.AddScoped<ShopTools>();

var app = builder.Build();

await app.RunAsync();
