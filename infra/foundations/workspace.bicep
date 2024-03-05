param location string = resourceGroup().location
param logAnalyticsName string

resource lanalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: 'AI Central Dashboard'
  location: location
  properties: {
    displayName: 'AI Central Dashboard'
    category: 'AI Central'
    version: '1.0'
    serializedData: loadTextContent('../dashboards/aicentral-dashboards.json')
  }
}

output logAnalyticsId string = lanalytics.id
