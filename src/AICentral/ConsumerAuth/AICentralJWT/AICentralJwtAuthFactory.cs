using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AICentral.Core;
using Microsoft.IdentityModel.Tokens;

namespace AICentral.ConsumerAuth.AICentralJWT;

public class AICentralJwtAuthFactory : IPipelineStepFactory
{
    private readonly string _policyId = Guid.NewGuid().ToString();
    private readonly string _stepName;
    private readonly AICentralJwtAuthProviderConfig _config;
    private bool _builtTokenDispatchRoute;
    private readonly SigningCredentials _signingCredentials;

    public AICentralJwtAuthFactory(string stepName, AICentralJwtAuthProviderConfig config)
    {
        _stepName = stepName;
        _config = config;
        var rsa = RSA.Create();
        rsa.ImportFromPem(config.PrivateKeyPem);

        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = "0"
        };

        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha512);
        _builtTokenDispatchRoute = false;
    }

    public void RegisterServices(IServiceCollection services)
    {
        var schemeName = $"AICentralJwt_{_policyId}";
        services.AddAuthentication().AddScheme<AICentralJwtAuthProviderConfig, AICentralJwtAuthenticationHandler>(
            schemeName, options =>
            {
                options.TokenIssuer = _config.TokenIssuer;
                options.PrivateKeyPem = _config.PrivateKeyPem;
                options.PublicKeyPem = _config.PublicKeyPem;
            });

        services.AddAuthorizationBuilder().AddPolicy(_policyId,
            policyBuilder => policyBuilder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName));
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return new AICentralJwtAuthProvider();
    }

    public object WriteDebug()
    {
        return new
        {
            auth = "AICentral JWT",
            path = $"/aicentraljwt/{_stepName}/tokens",
            issuer = _config.TokenIssuer,
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_policyId);

        if (_builtTokenDispatchRoute) return;

        app.MapPost("/aicentraljwt/{_stepName}/tokens", async (HttpContext context, TokenRequest request) =>
        {
            var apiKey = context.Request.Headers.TryGetValue("api-key", out var key);
            if (!apiKey) return Results.Unauthorized();
            if (key.ToString() != _config.AdminKey) return Results.Unauthorized();
            if (request.ValidPipelines.Any(x => !_config.ValidPipelines.Contains(x, StringComparer.InvariantCultureIgnoreCase))) return Results.BadRequest("This provider can only issue tokens for the following pipelines: " + string.Join(", ", _config.ValidPipelines));

            var issuer = _config.TokenIssuer;
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokens = request.Names.Select(x =>
            {
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, x),
                        new Claim(ClaimTypes.NameIdentifier, x),
                        new Claim("pipelines", string.Join(' ', request.ValidPipelines)),
                    }),
                    Expires = DateTime.UtcNow.Add(request.ValidFor),
                    Issuer = issuer,
                    Audience = issuer,
                    SigningCredentials = _signingCredentials,
                    IssuedAt = DateTime.UtcNow,
                    NotBefore = DateTime.UtcNow.AddSeconds(-10),
                };
                return new AICentralApiKeyToken()
                {
                    Client = x,
                    ApiKeyToken = tokenHandler.WriteToken(tokenHandler.CreateJwtSecurityToken(tokenDescriptor))
                };
            });

            return Results.Ok(new AICentralJwtProviderResponse
            {
                Tokens = tokens.ToArray()
            });
        });

        _builtTokenDispatchRoute = true;
    }

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var aiCentralJwtAuthProviderConfig = config.TypedProperties<AICentralJwtAuthProviderConfig>();

        Guard.NotNullOrEmptyOrWhitespace(aiCentralJwtAuthProviderConfig.AdminKey,
            nameof(aiCentralJwtAuthProviderConfig.AdminKey));

        Guard.NotNull(aiCentralJwtAuthProviderConfig.ValidPipelines,
            nameof(aiCentralJwtAuthProviderConfig.ValidPipelines));

        if (string.IsNullOrEmpty(aiCentralJwtAuthProviderConfig.PrivateKeyPem))
        {
            logger.LogWarning("No Private Key found in config for {AuthStepName}, generating a random one. ",
                config.Name);
        }
        else if (string.IsNullOrEmpty(aiCentralJwtAuthProviderConfig.PublicKeyPem))
        {
            logger.LogWarning("No Public Key found in config for {AuthStepName}, generating a random one. ",
                config.Name);
        }

        if (string.IsNullOrEmpty(aiCentralJwtAuthProviderConfig.PrivateKeyPem) ||
            string.IsNullOrEmpty(aiCentralJwtAuthProviderConfig.PublicKeyPem))
        {
            //make a new x509certificate2
            var rsa = RSA.Create();

            aiCentralJwtAuthProviderConfig = new AICentralJwtAuthProviderConfig()
            {
                AdminKey = aiCentralJwtAuthProviderConfig.AdminKey,
                TokenIssuer = aiCentralJwtAuthProviderConfig.TokenIssuer,
                PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
                PublicKeyPem = rsa.ExportRSAPublicKeyPem(),
                ValidPipelines =  aiCentralJwtAuthProviderConfig.ValidPipelines
            };
        }

        return new AICentralJwtAuthFactory(config.Name!, aiCentralJwtAuthProviderConfig);
    }

    public static string ConfigName => "AICentralJWT";
}

public class AICentralJwtProviderResponse
{
    public AICentralApiKeyToken[] Tokens { get; set; } = default!;
}

public class AICentralApiKeyToken
{
    public string Client { get; set; } = default!;
    public string ApiKeyToken { get; set; } = default!;
}