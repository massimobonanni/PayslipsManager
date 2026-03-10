@description('Name of the Web App.')
param name string

@description('Location for the resource.')
param location string

@description('Tags to apply to the resource.')
param tags object = {}

@description('Resource ID of the App Service Plan.')
param appServicePlanId string

@description('Application Insights connection string.')
param applicationInsightsConnectionString string

@description('Storage Account blob endpoint URL.')
param storageAccountBlobEndpoint string

@description('Container prefix for payslip blob containers.')
param containerPrefix string = 'payslips'

@description('Azure AD domain.')
param azureAdDomain string = ''

@description('Azure AD tenant ID.')
param azureAdTenantId string = ''

@description('Azure AD client ID.')
param azureAdClientId string = ''

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: name
  location: location
  tags: tags
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
        {
          name: 'AzureAd__Instance'
          value: environment().authentication.loginEndpoint
        }
        {
          name: 'AzureAd__Domain'
          value: azureAdDomain
        }
        {
          name: 'AzureAd__TenantId'
          value: azureAdTenantId
        }
        {
          name: 'AzureAd__ClientId'
          value: azureAdClientId
        }
        {
          name: 'AzureAd__CallbackPath'
          value: '/signin-oidc'
        }
        {
          name: 'AzureAd__SignedOutCallbackPath'
          value: '/signout-callback-oidc'
        }
      ]
    }
  }
}

output id string = webApp.id
output name string = webApp.name
output url string = 'https://${webApp.properties.defaultHostName}'
output identityPrincipalId string = webApp.identity.principalId
