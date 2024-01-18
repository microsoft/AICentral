param aspId string
// param privateEndpointSubnetId string
// param privateDnsZoneId string
param vnetIntegrationSubnetId string
param appName string
param appInsightsConnectionString string
param logAnalyticsId string
param location string = resourceGroup().location
param openAiEndpoint string
param managedIdentityId string
param kvName string
param azureMonitorWorkspaceName string
param key1SecretName string
param key2SecretName string
param workspaceKeySecretName string

resource monitorWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: azureMonitorWorkspaceName
}

resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    httpsOnly: true
    serverFarmId: aspId
    vnetRouteAllEnabled: true
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    clientAffinityEnabled: false
    keyVaultReferenceIdentity: managedIdentityId
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      vnetRouteAllEnabled: true
      ipSecurityRestrictions: []
      scmIpSecurityRestrictions: []
      linuxFxVersion: 'DOCKER|graemefoster/aicentral:latest'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'WEBSITES_PORT'
          value: '8080'
        }
        {
          name: 'AICentral__Endpoints__0__Type'
          value: 'AzureOpenAIEndpoint'
        }
        {
          name: 'AICentral__Endpoints__0__Name'
          value: 'aoai-1'
        }
        {
          name: 'AICentral__Endpoints__0__Properties__LanguageEndpoint'
          value: openAiEndpoint
        }
        {
          name: 'AICentral__Endpoints__0__Properties__AuthenticationType'
          value: 'entra'
        }
        {
          name: 'AICentral__AuthProviders__0__Type'
          value: 'ApiKey'
        }
        {
          name: 'AICentral__AuthProviders__0__Name'
          value: 'key-auth'
        }
        {
          name: 'AICentral__AuthProviders__0__Properties__Clients__0__ClientName'
          value: 'Client-1'
        }
        {
          name: 'AICentral__AuthProviders__0__Properties__Clients__0__Key1'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${key1SecretName})'
        }
        {
          name: 'AICentral__AuthProviders__0__Properties__Clients__0__Key2'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${key2SecretName})'
        }
        {
          name: 'AICentral__GenericSteps__0__Type'
          value: 'AspNetCoreFixedWindowRateLimiting'
        }
        {
          name: 'AICentral__GenericSteps__0__Name'
          value: 'token-rate-limiter'
        }
        {
          name: 'AICentral__GenericSteps__0__Properties__LimitType'
          value: 'PerAICentralEndpoint'
        }
        {
          name: 'AICentral__GenericSteps__0__Properties__MetricType'
          value: 'Tokens'
        }
        {
          name: 'AICentral__GenericSteps__0__Properties__Options__Window'
          value: '00:01:00'
        }
        {
          name: 'AICentral__GenericSteps__0__Properties__Options__PermitLimit'
          value: '10000'
        }
        {
          name: 'AICentral__GenericSteps__1__Type'
          value: 'AzureMonitorLogger'
        }
        {
          name: 'AICentral__GenericSteps__1__Name'
          value: 'azure-monitor-logger'
        }
        {
          name: 'AICentral__GenericSteps__1__Properties__WorkspaceId'
          value: monitorWorkspace.id
        }
        {
          name: 'AICentral__GenericSteps__1__Properties__Key'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${workspaceKeySecretName})'
        }
        {
          name: 'AICentral__GenericSteps__1__Properties__LogPrompt'
          value: 'true'
        }
        {
          name: 'AICentral__GenericSteps__1__Properties__LogResponse'
          value: 'true'
        }
        {
          name: 'AICentral__EndpointSelectors__0__Type'
          value: 'SingleEndpoint'
        }
        {
          name: 'AICentral__EndpointSelectors__0__Name'
          value: 'single-endpoint'
        }
        {
          name: 'AICentral__EndpointSelectors__0__Properties__Endpoint'
          value: 'aoai-1'
        }
        {
          name: 'AICentral__Pipelines__0__Name'
          value: 'single-endpoint'
        }
        {
          name: 'AICentral__Pipelines__0__Host'
          value: '${appName}.azurewebsites.net'
        }
        {
          name: 'AICentral__Pipelines__0__EndpointSelector'
          value: 'single-endpoint'
        }
        {
          name: 'AICentral__Pipelines__0__AuthProvider'
          value: 'key-auth'
        }
        {
          name: 'AICentral__Pipelines__0__Steps__0'
          value: 'token-rate-limiter'
        }
        {
          name: 'AICentral__Pipelines__0__Steps__1'
          value: 'azure-monitor-logger'
        }
      ]
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: app
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
      {
        category: 'AppServiceAuditLogs'
        enabled: true
      }
      {
        category: 'AppServicePlatformLogs'
        enabled: true
      }
    ]
  }
}

// resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
//   name: '${sampleAppName}-private-endpoint'
//   location: location
//   properties: {
//     subnet: {
//       id: privateEndpointSubnetId
//     }
//     privateLinkServiceConnections: [
//       {
//         name: '${sampleAppName}-private-link-service-connection'
//         properties: {
//           privateLinkServiceId: app.id
//           groupIds: [
//             'sites'
//           ]
//         }
//       }
//     ]
//   }

//   resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
//     name: '${sampleAppName}-private-endpoint-dns'
//     properties: {
//       privateDnsZoneConfigs: [
//         {
//           name: '${sampleAppName}-private-endpoint-cfg'
//           properties: {
//             privateDnsZoneId: privateDnsZoneId
//           }
//         }
//       ]
//     }
//   }
// }

output appUrl string = 'https://${app.properties.defaultHostName}/'
