using AICentral.Configuration;
using Serilog;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo
    .Console()
    .CreateLogger();
builder.Host.UseSerilog(logger);

builder.Services.AddAICentral(
    builder.Configuration,
    startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"));

var app = builder.Build();

app.UseAICentral();

app.Run();