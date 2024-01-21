param location string = resourceGroup().location
param logAnalyticsName string
param aspName string

resource lanalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'web'
  location: location
  name: '${aspName}-appinsights'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'BlueField'
    WorkspaceResourceId: lanalytics.id
    RetentionInDays: 30
  }
}

resource asp 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: aspName
  location: location
  kind: ''
  sku: {
    name: 'S1'
    capacity: 1
  }
  properties: {
    zoneRedundant: false
    reserved: true
  }
}

output applicationInsightsName string = appInsights.name
output aspId string = asp.id
output logAnalyticsId string = lanalytics.id
output logAnalyticsName string = lanalytics.name
