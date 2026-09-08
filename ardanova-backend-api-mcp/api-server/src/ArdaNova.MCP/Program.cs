using ArdaNova.Application;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.MCP.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true);

// The economy gate, enforced at BOTH host seams. ArdaNova.API refuses to start a Production
// host on a simulated or disabled economy; this host takes the SAME Azoa and Algorand settings
// and would otherwise serve real tools against fake money. Resolve and refuse BEFORE the
// container is built, so nothing is constructed on a mode that is not allowed to run.
// The banner goes to stderr: stdout is the MCP transport and must carry protocol only.
var economyMode = EconomyModeResolution.ResolveFromConfiguration(builder.Configuration);
EconomyModeResolution.ThrowIfNotLiveInProduction(economyMode, builder.Environment.IsProduction());
Console.Error.WriteLine(EconomyModeResolution.DescribeBanner(economyMode));

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

// Add MCP server with tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(UserTools).Assembly);

// Register tool types for DI
builder.Services.AddScoped<UserTools>();
builder.Services.AddScoped<ProjectTools>();
builder.Services.AddScoped<GuildTools>();
builder.Services.AddScoped<MembershipCredentialTools>();

var app = builder.Build();

await app.RunAsync();
