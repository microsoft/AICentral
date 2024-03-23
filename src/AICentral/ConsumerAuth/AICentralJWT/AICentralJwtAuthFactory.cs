using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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
        return new AICentralJwtAuthProvider(_config.ValidPipelines);
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

        app.MapPost($"/aicentraljwt/{_stepName}/tokens", (HttpContext context, TokenRequest request) =>
        {
            var apiKey = context.Request.Headers.TryGetValue("api-key", out var key);
            if (!apiKey) return Task.FromResult(Results.Unauthorized());
            if (key.ToString() != _config.AdminKey) return Task.FromResult(Results.Unauthorized());


            if (request.ValidPipelines == null || request.ValidFor == null)
            {
                return Task.FromResult(Results.BadRequest("Invalid request"));
            }

            if (request.ValidPipelines.Count == 0 )
            {
                return Task.FromResult(Results.BadRequest("Invalid request"));
            }
            
            if (request.ValidFor < TimeSpan.FromMinutes(5))
            {
                return Task.FromResult(Results.BadRequest("There is a minimum Valid timespan of 5 minutes for tokens"));
            }
            
            //ensure the combination is valid
            foreach (var requestedPipeline in request.ValidPipelines)
            {
                var valid = false;
                if (_config.ValidPipelines.TryGetValue(requestedPipeline.Key, out var pipeline))
                {
                    valid = pipeline.Contains("*") || requestedPipeline.Value.All(p =>
                        pipeline.Contains(p, StringComparer.InvariantCultureIgnoreCase));
                    
                }
                if (!valid)
                {
                    return Task.FromResult(Results.BadRequest($"Unable to issue JWT for Pipeline {requestedPipeline.Key} / Deployments '{string.Join(", ", requestedPipeline.Value)}'"));
                }
            }
            
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
                        new Claim("pipelines", JsonSerializer.Serialize(request.ValidPipelines)),
                    }),
                    Expires = DateTime.UtcNow.Add(request.ValidFor.Value),
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

            return Task.FromResult(Results.Ok(new AICentralJwtProviderResponse
            {
                Tokens = tokens.ToArray()
            }));
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