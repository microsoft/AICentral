using System.Reflection;

namespace AICentralTests.TestHelpers.FakeIdp;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

/// <summary>
/// Used to fake an IdP so we can test JWT scenarios
/// </summary>
public class FakeIdpMessageHandler : DelegatingHandler
{
    public static readonly RsaSecurityKey Key = new(RSA.Create(2048));
    public static readonly string TenantId = Guid.NewGuid().ToString().ToLowerInvariant();
    public static readonly string FakeAppId = Guid.NewGuid().ToString().ToLowerInvariant();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Request for {request.RequestUri}");
        
        if (request.RequestUri!.AbsoluteUri ==
            $"https://login.microsoftonline.com/{TenantId}/v2.0/.well-known/openid-configuration")
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            using var resourceStream =
                new StreamReader(
                typeof(FakeIdpMessageHandler).Assembly.GetManifestResourceStream(
                    "AICentral.TestHelpers.TestHelpers.FakeIdp.openid-configuration.json")!);

            var content = (await resourceStream.ReadToEndAsync(cancellationToken))
                .Replace("<tenant-id>", TenantId);

            response.Content = new ByteArrayContent(Encoding.UTF8.GetBytes( content));
            return response;
        }


        if (request.RequestUri.AbsoluteUri == $"https://login.microsoftonline.com/{TenantId}/discovery/v2.0/keys")
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        keys = new[]
                        {
                            JsonWebKeyConverter.ConvertFromSecurityKey(Key)
                        }
                    })
                ));
            return response;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}