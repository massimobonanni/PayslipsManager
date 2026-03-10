@description('Name of the Storage Account for Event Grid subscription.')
param storageAccountName string

@description('Resource ID of the Function App.')
param functionAppId string

@description('Container prefix for filtering events.')
param containerPrefix string = 'payslips'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource eventGridSubscription 'Microsoft.EventGrid/eventSubscriptions@2024-06-01-preview' = {
  name: 'payslip-blob-created'
  scope: storageAccount
  properties: {
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        resourceId: '${functionAppId}/functions/PayslipBlobCreatedFunction'
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
      ]
      subjectBeginsWith: '/blobServices/default/containers/${containerPrefix}'
      isSubjectCaseSensitive: false
    }
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}
