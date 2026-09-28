targetScope = 'resourceGroup'

@description('Existing Cosmos DB account name.')
param cosmosAccountName string

@description('Existing Cosmos SQL database name.')
param cosmosDatabaseName string

@description('Existing Service Bus namespace name.')
param serviceBusNamespaceName string

@description('Existing Storage account name.')
param storageAccountName string

@description('Manifest job ledger container.')
param manifestJobsContainerName string = 'manifestJobs'

@description('Canonical row-error container.')
param manifestRowErrorsContainerName string = 'manifestRowErrors'

@description('Downstream parcel-ingest queue.')
param parcelIngestQueueName string = 'parcel-ingest'

@description('Durable source-artifact blob container.')
param manifestUploadContainerName string = 'manifest-uploads'

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosAccountName
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' existing = {
  parent: cosmos
  name: cosmosDatabaseName
}

resource manifestJobs 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: manifestJobsContainerName
  properties: {
    resource: {
      id: manifestJobsContainerName
      partitionKey: {
        paths: [
          '/tenantId'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource manifestRowErrors 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: manifestRowErrorsContainerName
  properties: {
    resource: {
      id: manifestRowErrorsContainerName
      partitionKey: {
        paths: [
          '/tenantId'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource parcelIngestQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: parcelIngestQueueName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource manifestUploads 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: manifestUploadContainerName
  properties: {
    publicAccess: 'None'
  }
}
