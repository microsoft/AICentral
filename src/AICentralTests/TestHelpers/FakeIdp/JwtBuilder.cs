namespace AICentralTests.TestHelpers.FakeIdp;

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

public class JwtBuilder
{
    private readonly RsaSecurityKey _metadataProviderKey;
    private string _tenantId;
    private readonly JsonWebTokenHandler _handler;
    private string[]? _scopes;
    private DateTime? _nbf;
    private DateTime? _expires;
    private DateTime? _iss;
    private string? _appId;
    private string? _user;
    private string? _aud;

    public JwtBuilder(RsaSecurityKey metadataProviderKey, string tenantId, string clientId)
    {
        _metadataProviderKey = metadataProviderKey;
        _tenantId = tenantId;
        _aud = clientId;
        _handler = new JsonWebTokenHandler();
    }

    public JwtBuilder WithScopes(params string[] scopes)
    {
        _scopes = scopes;
        return this;
    }

    public JwtBuilder FromTenantId(string tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public JwtBuilder IssuedToUser(string user)
    {
        _user = user;
        return this;
    }

    public JwtBuilder IssuedToAppId(string appId)
    {
        _appId = appId;
        return this;
    }

    public JwtBuilder WithAudience(string aud)
    {
        _aud = aud;
        return this;
    }

    public JwtBuilder WithCustomDates(DateTime nbf, DateTime expires, DateTime iss)
    {
        _nbf = nbf;
        _expires = expires;
        _iss = iss;
        return this;
    }

    public string Build()
    {
        var token = new SecurityTokenDescriptor()
        {
            Issuer = $"https://login.microsoftonline.com/{_tenantId}/v2.0",
            Audience = _aud,
            Expires = _expires ?? DateTime.Now.AddHours(1),
            NotBefore = _nbf ?? DateTime.Now.AddHours(-1),
            IssuedAt = _iss ?? DateTime.Now.AddMinutes(-1),
            SigningCredentials = new SigningCredentials(_metadataProviderKey, SecurityAlgorithms.RsaSsaPssSha512),
            Claims = new Dictionary<string, object>()
            {
                ["ver"] = "2.0",
                ["tid"] = _tenantId,
                ["scp"] = string.Join(" ", _scopes ?? [])
            }
        };
        if (_appId != null) token.Claims["appId"] = _appId;
        if (_user != null) token.Subject = new GenericIdentity(_user);
        if (_aud != null) token.Audience = _aud;
        
        return _handler.CreateToken(token);

    }
}