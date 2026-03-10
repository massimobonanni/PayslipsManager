@description('Name of the Function App.')
param name string

@description('Location for the resource.')
param location string

@description('Tags to apply to the resource.')
param tags object = {}

@description('Resource ID of the App Service Plan.')
param appServicePlanId string

@description('Application Insights connection string.')
param applicationInsightsConnectionString string

@description('Storage Account name.')
param storageAccountName string

@description('Storage Account blob endpoint URL.')
param storageAccountBlobEndpoint string

@description('Container prefix for payslip blob containers.')
param containerPrefix string = 'payslips'

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'BlobStorage__AccountUrl'
          value: storageAccountBlobEndpoint
        }
        {
          name: 'BlobStorage__ContainerPrefix'
          value: containerPrefix
        }
        {
          name: 'BlobStorage__UseManagedIdentity'
          value: 'true'
        }
      ]
    }
  }
}

output id string = functionApp.id
output name string = functionApp.name
output url string = 'https://${functionApp.properties.defaultHostName}'
output identityPrincipalId string = functionApp.identity.principalId
