targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g., dev, staging, prod). Used as a suffix for resource names.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Name of the Azure AD tenant domain (e.g., contoso.onmicrosoft.com).')
param azureAdDomain string = ''

@description('Azure AD tenant ID for authentication.')
param azureAdTenantId string = ''

@description('Azure AD client ID for the web application.')
param azureAdClientId string = ''

@description('Container prefix for payslip blob containers.')
param containerPrefix string = 'payslips'

// Generate a unique token for resource naming
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// Log Analytics workspace (required by Application Insights)
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalytics'
  scope: rg
  params: {
    name: 'log-${resourceToken}'
    location: location
    tags: tags
  }
}

// Application Insights
module applicationInsights 'modules/application-insights.bicep' = {
  name: 'applicationInsights'
  scope: rg
  params: {
    name: 'appi-${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// Storage Account (Standard GPv2)
module storageAccount 'modules/storage-account.bicep' = {
  name: 'storageAccount'
  scope: rg
  params: {
    name: 'st${resourceToken}'
    location: location
    tags: tags
  }
}

// App Service Plan (shared by Web App and Function App)
module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'appServicePlan'
  scope: rg
  params: {
    name: 'plan-${resourceToken}'
    location: location
    tags: tags
  }
}

// Web App
module webApp 'modules/web-app.bicep' = {
  name: 'webApp'
  scope: rg
  params: {
    name: 'app-web-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'web' })
    appServicePlanId: appServicePlan.outputs.id
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    storageAccountBlobEndpoint: storageAccount.outputs.blobEndpoint
    containerPrefix: containerPrefix
    azureAdDomain: azureAdDomain
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
  }
}

// Function App
module functionApp 'modules/function-app.bicep' = {
  name: 'functionApp'
  scope: rg
  params: {
    name: 'func-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'functions' })
    appServicePlanId: appServicePlan.outputs.id
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    storageAccountName: storageAccount.outputs.name
    storageAccountBlobEndpoint: storageAccount.outputs.blobEndpoint
    containerPrefix: containerPrefix
  }
}

// Role assignments: Web App managed identity -> Storage Blob Data Reader
module webAppBlobRole 'modules/role-assignment.bicep' = {
  name: 'webAppBlobRole'
  scope: rg
  params: {
    principalId: webApp.outputs.identityPrincipalId
    roleDefinitionId: '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1' // Storage Blob Data Reader
    storageAccountName: storageAccount.outputs.name
    principalType: 'ServicePrincipal'
  }
}

// Role assignments: Function App managed identity -> Storage Blob Data Contributor
module functionAppBlobRole 'modules/role-assignment.bicep' = {
  name: 'functionAppBlobRole'
  scope: rg
  params: {
    principalId: functionApp.outputs.identityPrincipalId
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
    storageAccountName: storageAccount.outputs.name
    principalType: 'ServicePrincipal'
  }
}

// Role assignments: Function App managed identity -> Storage Account Contributor (for AzureWebJobsStorage)
module functionAppStorageRole 'modules/role-assignment.bicep' = {
  name: 'functionAppStorageRole'
  scope: rg
  params: {
    principalId: functionApp.outputs.identityPrincipalId
    roleDefinitionId: '17d1049b-9a84-46fb-8f53-869881c3d3ab' // Storage Account Contributor
    storageAccountName: storageAccount.outputs.name
    principalType: 'ServicePrincipal'
  }
}

// Event Grid subscription for BlobCreated events
module eventGridSubscription 'modules/event-grid.bicep' = {
  name: 'eventGridSubscription'
  scope: rg
  params: {
    storageAccountName: storageAccount.outputs.name
    functionAppId: functionApp.outputs.id
    containerPrefix: containerPrefix
  }
}

// Outputs required by azd
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name
output WEB_APP_NAME string = webApp.outputs.name
output WEB_APP_URL string = webApp.outputs.url
output FUNCTION_APP_NAME string = functionApp.outputs.name
output STORAGE_ACCOUNT_NAME string = storageAccount.outputs.name
output APPLICATIONINSIGHTS_CONNECTION_STRING string = applicationInsights.outputs.connectionString
