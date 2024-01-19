param vnetName string
param location string = resourceGroup().location
param vnetCidr string

resource vnet 'Microsoft.Network/virtualNetworks@2022-11-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [vnetCidr]
    }
    subnets: [
      {
        name: 'AppServiceDelegated'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 1)
          delegations: [
            {
              name: 'AppServiceDelegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'PrivateEndpoints'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 2)
          privateEndpointNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

resource gwayNsg 'Microsoft.Network/networkSecurityGroups@2022-11-01' = {
  name: '${vnetName}-gway-nsg'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowIncomingFromInternetToAICentral'
        properties: {
          access: 'Allow'
          direction: 'Inbound'
          priority: 1000
          protocol: 'Tcp'
          description: 'Let AI Central traffic in'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}


resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurewebsites.net'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.azurewebsites.net-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource cosmosPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.documents.azure.com'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.documents.azure.com-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource openAiPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.openai.azure.com'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.openai.azure.com-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource keyvaultPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'vaultcore.azure.net'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.vaultcore.azure.net'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource cogSearchPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.search.windows.net'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.search.windows.net-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource storagePrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.${environment().suffixes.storage}'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.blob.${environment().suffixes.storage}-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

output privateEndpointSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'PrivateEndpoints')[0].id
output vnetIntegrationSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'AppServiceDelegated')[0].id
output privateDnsZoneId string = privateDnsZone.id
output openAiPrivateDnsZoneId string = openAiPrivateDnsZone.id
output cosmosPrivateDnsZoneId string = cosmosPrivateDnsZone.id
output cogSearchPrivateDnsZoneId string = cogSearchPrivateDnsZone.id
output storagePrivateDnsZoneId string =  storagePrivateDnsZone.id
output keyvaultPrivateDnsZoneId string =  keyvaultPrivateDnsZone.id
