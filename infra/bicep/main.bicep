@description('リソースの環境名（dev, staging, prod）')
param environment string = 'dev'

@description('デプロイするAzureリージョン')
param location string = resourceGroup().location

@description('アプリケーション名のプレフィックス')
param appName string = 'kids-money-note'

// リソース名の変数定義
var resourceNames = {
  containerAppsEnvironment: 'cae-${appName}-${environment}'
  logAnalytics: 'log-${appName}-${environment}'
  applicationInsights: 'appi-${appName}-${environment}'
  containerRegistry: 'cr${appName}${environment}'
  sqlServer: 'sql-${appName}-${environment}'
  sqlDatabase: 'sqldb-${appName}-${environment}'
  keyVault: 'kv-${appName}-${environment}'
  serviceBus: 'sb-${appName}-${environment}'
  storageAccount: 'st${appName}${environment}'
}

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: resourceNames.logAnalytics
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environment == 'prod' ? 90 : 30
    features: {
      immediatePurgeDataOn30Days: true
    }
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: resourceNames.applicationInsights
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: resourceNames.containerAppsEnvironment
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: resourceNames.sqlServer
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: 'YourStrong@Passw0rd' // 実際には Key Vault から取得
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}

// Azure SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: resourceNames.sqlDatabase
  location: location
  sku: {
    name: environment == 'prod' ? 'S2' : 'S0'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: environment == 'prod' ? 268435456000 : 2147483648 // 250GB for prod, 2GB for dev
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: resourceNames.containerRegistry
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard' : 'Basic'
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: resourceNames.serviceBus
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard' : 'Basic'
    tier: environment == 'prod' ? 'Standard' : 'Basic'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

// Service Bus Topics
var serviceBusTopics = [
  'user-events'
  'account-events'
  'transaction-events'
  'goal-events'
  'notification-events'
]

resource serviceBusTopicsResource 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = [for topic in serviceBusTopics: {
  parent: serviceBusNamespace
  name: topic
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    supportOrdering: true
    autoDeleteOnIdle: 'P10675199DT2H48M5.4775807S'
    enablePartitioning: false
    enableExpress: false
  }
}]

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: resourceNames.keyVault
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: []
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: resourceNames.storageAccount
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard_LRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    defaultToOAuthAuthentication: false
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

// 出力値
output containerAppsEnvironmentId string = containerAppsEnvironment.id
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output serviceBusNamespace string = serviceBusNamespace.name
output keyVaultName string = keyVault.name
output storageAccountName string = storageAccount.name