using AICentral.Configuration;
using Serilog;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo
    .Console()
    .CreateLogger();

builder.Host.UseSerilog(logger);

builder.Services.AddAICentral(
    builder.Configuration,
    startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"));

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapRazorPages();

app.UseAICentral();

app.Run();

public partial class Program
{ }
