using AICentral.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies: new []
    {
        typeof(Program).Assembly
    });

var app = builder.Build();

app.Map("/health", () => "OK");
app.UseAICentral();

app.Run();