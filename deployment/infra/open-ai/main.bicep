targetScope = 'resourceGroup'
param openAiResourceName string
param openAiModelName string
param openAiEmbeddingModelName string
param managedIdentityPrincipalId string
param openAiLocation string
param privateEndpointSubnetId string
param privateDnsZoneId string

resource openAi 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: openAiResourceName
  location: openAiLocation
  kind: 'OpenAI'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  properties: {
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: []
    }
    customSubDomainName: openAiResourceName
  }
}

resource deploymentNew 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  name: openAiModelName
  parent: openAi
  sku: {
    name: 'Standard'
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
  }
}

resource embeddingDeploymentNew 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  name: openAiEmbeddingModelName
  parent: openAi
  sku: {
    name: 'Standard'
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  dependsOn: [
    deploymentNew
  ]
}

resource openAiRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

resource rbacModelReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('${managedIdentityPrincipalId}-search-${openAi.id}')
  scope: openAi
  properties: {
    roleDefinitionId: openAiRole.id
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: 'openai-private-endpoint'
  location: openAiLocation
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'openai-private-link-service-connection'
        properties: {
          privateLinkServiceId: openAi.id
          groupIds: [
            'account'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: 'openai-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'openai-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: privateDnsZoneId
          }
        }
      ]
    }
  }
}

output id string = openAi.id
output openAiName string = openAi.name
output openAiEndpoint string = openAi.properties.endpoint
output modelName string = deploymentNew.name
output embeddingModelName string = embeddingDeploymentNew.name
