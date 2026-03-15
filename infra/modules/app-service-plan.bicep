@description('Name of the App Service Plan.')
param name string

@description('Location for the resource.')
param location string

@description('Tags to apply to the resource.')
param tags object = {}

@description('SKU name for the App Service Plan.')
param skuName string = 'F1'

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    reserved: false
  }
}

output id string = appServicePlan.id
output name string = appServicePlan.name
