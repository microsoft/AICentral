targetScope = 'subscription'

// The main bicep module to provision Azure resources.
// For a more complete walkthrough to understand how this file works with azd,
// see https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/make-azd-compatible?pivots=azd-create

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@minLength(1)
@description('Key 1 for the AI Central OpenAI consumer')
param key1 string

@minLength(1)
@description('Key 2 for the AI Central OpenAI consumer')
param key2 string

param resourceGroupName string = ''

var abbrs = loadJsonContent('./abbreviations.json')

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
}

// Generate a unique token to be used in naming resources.
// Remove linter suppression after using.
#disable-next-line no-unused-vars
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Name of the service defined in azure.yaml
// A tag named azd-service-name with this value should be applied to the service host resource, such as:
//   Microsoft.Web/sites for appservice, function
// Example usage:
//   tags: union(tags, { 'azd-service-name': apiServiceName })
#disable-next-line no-unused-vars

var kvName = '${abbrs.keyVaultVaults}${resourceToken}'
var aspName = 'asp-${resourceToken}'
var aiCentralAppName = '${abbrs.webSitesAppService}${resourceToken}-aic'
var openAiName = toLower('${abbrs.cognitiveServicesAccounts}${resourceToken}')
var lanalytics = '${abbrs.operationalInsightsWorkspaces}${resourceToken}-logs'

var key1SecretName = 'key1'
var key2SecretName = 'key2'
var workspaceKeySecretName = 'workspaceKey'

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

module vnet 'foundations/network.bicep' = {
  name: '${deployment().name}-vnet'
  scope: rg
  params: {
    vnetCidr: '10.0.0.0/12'
    vnetName: '${abbrs.networkVirtualNetworks}vnet'
    location: location
  }
}

module managedIdentities 'foundations/identities.bicep' = {
  name: '${deployment().name}-identities'
  scope: rg
  params: {
    location: location
  }
}

module asp 'foundations/asp.bicep' = {
  name: '${deployment().name}-foudndations'
  scope: rg
  params: {
    aspName: aspName
    logAnalyticsName: lanalytics
    location: location
  }
}

module kv 'foundations/keyvault.bicep' = {
  name: '${deployment().name}-kv'
  scope: rg
  params: {
    location: location
    kvName: kvName
    logAnalyticsName: asp.outputs.logAnalyticsName
    appServicePrincipalId: managedIdentities.outputs.aspIdentityPrincipalId
    key1SecretName: key1SecretName
    key2SecretName: key2SecretName
    workspaceKeySecretName: workspaceKeySecretName
    key1: key1
    key2: key2
    privateDnsZoneId: vnet.outputs.keyvaultPrivateDnsZoneId
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
  }
}

module openAi 'open-ai/main.bicep' = {
  name: '${deployment().name}-openai'
  scope: rg
  params: {
    openAiLocation: location
    openAiModelName: 'Gpt35Turbo0613'
    openAiEmbeddingModelName: 'Ada002Embedding'
    openAiResourceName: openAiName
    privateDnsZoneId: vnet.outputs.openAiPrivateDnsZoneId
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
  }
}

module aicentral 'webapp/aicentral.bicep' = {
  name: '${deployment().name}-apps'
  scope: rg
  params: {
    location: location
    appInsightsName: asp.outputs.applicationInsightsName
    aspId: asp.outputs.aspId
    logAnalyticsId: asp.outputs.logAnalyticsId
    appName: aiCentralAppName
    vnetIntegrationSubnetId: vnet.outputs.vnetIntegrationSubnetId
    azureMonitorWorkspaceName: asp.outputs.logAnalyticsName
    key1SecretName: key1SecretName
    key2SecretName: key2SecretName
    workspaceKeySecretName: workspaceKeySecretName
    kvName: kv.outputs.kvName
    openAiEndpoint: openAi.outputs.openAiEndpoint
    // privateDnsZoneId: vnet.outputs.privateDnsZoneId
    // privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
    managedIdentityId: managedIdentities.outputs.aspIdentityId
    openAiName: openAiName
  }
}

// Add outputs from the deployment here, if needed.
//
// This allows the outputs to be referenced by other bicep deployments in the deployment pipeline,
// or by the local machine as a way to reference created resources in Azure for local development.
// Secrets should not be added here.
//
// Outputs are automatically saved in the local azd environment .env file.
// To see these outputs, run `azd env get-values`,  or `azd env get-values --output json` for json output.
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AI_CENTRAL_PROXY_URL string = aicentral.outputs.appUrl
