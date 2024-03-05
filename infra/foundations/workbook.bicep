param location string = resourceGroup().location
param logAnalyticsName string

resource lanalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsName
}


resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid(resourceGroup().name, 'aicentraldashboard')
  location: location
  kind: 'shared'
  properties: {
    displayName: 'AI Central Dashboard'
    category: 'workbook'
    version: '1.0'
    serializedData: loadTextContent('../dashboards/aicentral-dashboards.json')
    sourceId: lanalytics.id
  }
}
