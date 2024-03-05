param kvName string
param location string = resourceGroup().location
param logAnalyticsName string
param appServicePrincipalId string
param key1SecretName string
param key2SecretName string
param workspaceKeySecretName string

param privateEndpointSubnetId string
param privateDnsZoneId string

@secure()
param key1 string

@secure()
param key2 string

var openAiSecretName = 'OpenAiKey'

resource lanalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsName
}

//used to store LetsEncrypt certificate we generate on post-hook
resource keyvault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  location: location
  name: kvName
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        objectId: appServicePrincipalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: [ 'get', 'list' ]
        }
      }
    ]
  }

  resource key1Secret 'secrets@2023-07-01' = {
    name: key1SecretName
    properties: {
      contentType: 'text/plan'
      value: key1
    }
  }

  resource key2Secret 'secrets@2023-07-01' = {
    name: key2SecretName
    properties: {
      contentType: 'text/plan'
      value: key2
    }
  }

  resource workspaceKeySecret 'secrets@2023-07-01' = {
    name: workspaceKeySecretName
    properties: {
      contentType: 'text/plan'
      value: lanalytics.listKeys().primarySharedKey
    }
  }
}

resource kvDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: keyvault
  name: 'diagnostics'
  properties: {
    workspaceId: lanalytics.id
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
    ]
  }
}


resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: 'keyvault-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'keyvault-private-link-service-connection'
        properties: {
          privateLinkServiceId: keyvault.id
          groupIds: [
            'vault'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: 'keyvault-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'keyvault-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: privateDnsZoneId
          }
        }
      ]
    }
  }
}

output kvName string = kvName
output kvId string = keyvault.id
output kvUri string = keyvault.properties.vaultUri
output openAiSecretName string = openAiSecretName
