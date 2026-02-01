targetScope = 'resourceGroup'

@description('Base name for all resources')
param baseName string = 'jumpmetrics'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Azure OpenAI model deployment name')
param openAiModelDeploymentName string = 'gpt-4'

// Storage Account - Blob (FlySight files) + Table (metrics cache)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${baseName}storage'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'

  resource blobService 'blobServices' = {
    name: 'default'

    resource flysightContainer 'containers' = {
      name: 'flysight-files'
    }
  }

  resource tableService 'tableServices' = {
    name: 'default'

    resource metricsTable 'tables' = {
      name: 'JumpMetrics'
    }
  }
}

// App Service Plan (Consumption)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${baseName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

// Function App (isolated worker)
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${baseName}-func'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value}' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
      ]
      netFrameworkVersion: 'v10.0'
    }
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// Azure OpenAI account
resource openAi 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: '${baseName}-openai'
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  properties: {}

  resource deployment 'deployments' = {
    name: openAiModelDeploymentName
    sku: {
      name: 'Standard'
      capacity: 10
    }
    properties: {
      model: {
        format: 'OpenAI'
        name: 'gpt-4'
        version: '0613'
      }
    }
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output functionAppName string = functionApp.name
output openAiEndpoint string = openAi.properties.endpoint
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
