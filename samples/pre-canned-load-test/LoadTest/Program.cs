using AICentral.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies: new []
    {
        typeof(Program).Assembly
    });

builder.Logging.AddConsole();

var app = builder.Build();

app.UseAICentral();

app.Run();