using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AICentral.Endpoints.AzureOpenAI;

/// <summary>
/// A check to help with common network issues we see when using Azure Open AI, especially wrt: TLS and private endpoints 
/// </summary>
public class AzureOpenAIDownstreamEndpointDiagnostics
{
    private readonly ILogger _logger;
    private readonly string _endpointName;
    private readonly Uri _languageUrl;

    public AzureOpenAIDownstreamEndpointDiagnostics(
        ILogger logger,
        string endpointName,
        Uri languageUrl)
    {
        _logger = logger;
        _endpointName = endpointName;
        _languageUrl = languageUrl;
    }

    public async Task RunDiagnostics()
    {
        _logger.LogInformation("Running Diagnostics check on Azure Open AI Endpoint: {Endpoint}",
            _endpointName);

        CheckHostName();
        if (CheckDNS())
        {
            if (await CheckConnectivity())
            {
                _logger.LogInformation("Successfully run Diagnostics check on Azure Open AI Endpoint: {Endpoint}, HostName: {HostName}. Host looks good.",
                    _endpointName, _languageUrl);
            }
        }
    }

    private void CheckHostName()
    {
        var hostName = _languageUrl.Host;
        var dodgy = false;

        if (hostName.Contains(".privatelink.", StringComparison.InvariantCultureIgnoreCase))
        {
            if (hostName.Contains(".privatelink.openai.com", StringComparison.InvariantCultureIgnoreCase))
            {
                dodgy = true;
                _logger.LogWarning(
                    "Detected potential mistake in LanguageEndpoint for Endpoint {Endpoint}. HostName {HostName} ends with '.privatelink.openai.azure.com'. When configuring Private Link, the Language Endpoint should still be the canonical Azure Open AI Hostname, e.g. https://<resource-name>.openai.azure.com/. Private Link uses DNS CNames to alias this hostname to '<resource-name>.privatelink.openai.azure.com'",
                    _endpointName,
                    _languageUrl.Host);
            }
            else
            {
                dodgy = true;
                _logger.LogWarning(
                    "Detected potential mistake in LanguageEndpoint for Endpoint {Endpoint}. HostName {HostName} includes '.privatelink'. When configuring Private Link, the Language Endpoint should still be the canonical Azure Open AI Hostname, e.g. https://<resource-name>.openai.azure.com/. Private Link uses DNS CNames to alias this hostname to '<resource-name>.privatelink.openai.azure.com'",
                    _endpointName,
                    _languageUrl.Host);
            }
        }

        if (!hostName.EndsWith("openai.azure.com"))
        {
            dodgy = true;
            _logger.LogWarning(
                "Detected potential mistake in LanguageEndpoint for Endpoint {Endpoint}. Hostname {HostName} does not end with openai.azure.com. For most scenarios the Language Endpoint should be the canonical Azure Open AI Hostname, e.g. https://<resource-name>.openai.azure.com/.",
                _endpointName, _languageUrl.Host);
        }

        if (hostName.StartsWith("http://"))
        {
            dodgy = true;
            _logger.LogWarning(
                "Detected potential mistake in LanguageEndpoint for Endpoint {Endpoint}. Hostname {HostName} starts with 'http://'. The Language Endpoint should be https.",
                _endpointName, _languageUrl.Host);
        }

        if (!dodgy)
        {
            _logger.LogInformation("Endpoint {Endpoint} with Hostname {HostName} is a valid Azure Open AI Host",
                _endpointName, _languageUrl);
        }
    }

    private bool CheckDNS()
    {
        try
        {
            var dnsResolve = Dns.GetHostAddresses(_languageUrl.Host);
            foreach (var ipAddress in dnsResolve)
            {
                _logger.LogInformation("Resolved {Endpoint} at HostName {HostName} to IP Address: {IpAddress}",
                    _endpointName, _languageUrl.Host,
                    ipAddress);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to resolve {Endpoint} at HostName {HostName} to an IP Address. Check if this Azure Open AI service exists as there are no DNS entries pointing to it.",
                _endpointName, _languageUrl.Host);
        }

        return false;
    }

    private async Task<bool> CheckConnectivity()
    {
        var host = _languageUrl.Host;
        var port = _languageUrl.Port;
        var dodgy = false;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        await using var sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

        try
        {
            await sslStream.AuthenticateAsClientAsync(host);
            _logger.LogInformation(
                "Successfully connected to {Endpoint} on Host {Host} with a valid SSL certificate.",
                _endpointName, host);
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Failed to authenticate SSL connection to {Host}. Error: {Error}", host, e.Message);
            dodgy = true;
        }

        return !dodgy;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        _logger.LogError("Detected Certificate errors when connecting to {Endpoint} on Host {Host}: {Error}",
            _endpointName, _languageUrl.Host, sslPolicyErrors);

        _logger.LogError(
            "Downstream Server for {Endpoint} on Host {Host} presented this certificate: {CertificateCN} with SANs:{SANS}",
            _endpointName, _languageUrl.Host, certificate?.Subject ?? "<no-certificate-presented>",
            string.Join(",", GetSubjectAlternativeNames(certificate as X509Certificate2)));

        return false;
    }

    private string[] GetSubjectAlternativeNames(X509Certificate2? certificate)
    {
        if (certificate == null) return [];

        var sanExtension = certificate.Extensions.FirstOrDefault(s => s is X509SubjectAlternativeNameExtension);
        if (sanExtension != null)
        {
            return ((X509SubjectAlternativeNameExtension)sanExtension).EnumerateDnsNames().ToArray();
        }

        return [];
    }
}