# AI Central

AI Central gives you control over your AI services.

- Intelligent Routing
- Custom consumer OAuth2 authorisation
- Fallback AI service
- Round Robin AI services
- Circuit breakers, and backoff-retry over downstream AI services

## ```appsettings.json``` Configuration

```json
{
  "ExposeTestPage": true,
  "AICentral": {
    "Endpoints": [
      {
        "Type": "AzureOpenAIEndpoint",
        "Name": "openai-1",
        "Properties": {
          "LanguageEndpoint": "https://<my-ai>.openai.azure.com",
          "ModelName": "<model-name>",
          "AuthenticationType": "Entra|EntraPassThrough|ApiKey"
        }
      }
    ],
    "AuthProviders": [
      {
        "Type": "Entra",
        "Name": "simple-aad",
        "Properties": {
          "ClientId": "<my-client-id>",
          "TenantId": "<my-tenant-id>",
          "Instance": "https://login.microsoftonline.com/",
          "Audience": "<custom-audience>"
        }
      }
    ],
    "EndpointSelectors": [
      {
        "Type": "SingleEndpoint",
        "Name": "default",
        "Properties": {
          "Endpoint": "openai-1"
        }
      }
    ],
    "Pipelines": [
      {
        "Name": "LoggedOpenAiPipeline",
        "Path": "/openai/deployments/Gpt35Turbo0613/chat/completions",
        "EndpointSelector": "default",
        "AuthProvider": "simple-aad",
        "Steps": [
          {
            "Type": "AzureMonitorLogger",
            "Properties": {
              "WorkspaceId": "<workspace-id>",
              "Key": "<workspace-key>",
              "LogPrompt": true
            }
          }
        ]
      }
    ]
  }
}

```

## Bicep App Service configuration
```bicep
resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: sampleAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: aspId
    vnetRouteAllEnabled: true
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    clientAffinityEnabled: false
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      vnetRouteAllEnabled: true
      ipSecurityRestrictions: []
      scmIpSecurityRestrictions: []
      linuxFxVersion: 'DOCKER|graemefoster/aicentral:0.5'
      appSettings: [
        {
          name: 'AICentral__Endpoints__0__Type'
          value: 'AzureOpenAIEndpoint'
        }
        {
          name: 'AICentral__Endpoints__0__Name'
          value: 'openai-1'
        }
        {
          name: 'AICentral__Endpoints__0__Properties__LanguageEndpoint'
          value: openAiUrl
        }
        {
          name: 'AICentral__Endpoints__0__Properties__ModelName'
          value: openAiModelName
        }
        {
          name: 'AICentral__Endpoints__0__Properties__AuthenticationType'
          value: 'EntraPassThrough'
        }
        {
          name: 'AICentral__EndpointSelectors__0__Type'
          value: 'SingleEndpoint'
        }
        {
          name: 'AICentral__EndpointSelectors__0__Name'
          value: 'default'
        }
        {
          name: 'AICentral__EndpointSelectors__0__Properties__Endpoint'
          value: 'openai-1'
        }
        {
          name: 'AICentral__Pipelines__0__Name'
          value: 'SynchronousPipeline'
        }
        {
          name: 'AICentral__Pipelines__0__Path'
          value: '/openai/deployments/Gpt35Turbo0613/chat/completions'
        }
        {
          name: 'AICentral__Pipelines__0__EndpointSelector'
          value: 'default'
        }
        {
          name: 'AICentral__Pipelines__0__Steps__0__Type'
          value: 'AzureMonitorLogger'
        }
        {
          name: 'AICentral__Pipelines__0__Steps__0__Properties__WorkspaceId'
          value: azureMonitorWorkspaceId
        }
        {
          name: 'AICentral__Pipelines__0__Steps__0__Properties__Key'
          value: listKeys(lanalytics.id, '2020-08-01').primarySharedKey
        }
        {
          name: 'AICentral__Pipelines__0__Steps__0__Properties__LogPrompt'
          value: 'true'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://index.docker.io/v1'
        }
      ]
    }
  }
}
```