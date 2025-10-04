# Azure インフラストラクチャ設計書

## 1. インフラストラクチャ概要

### 1.1 Azureリソース構成図

```
Azure Subscription
├── Resource Group: rg-kids-money-note-prod
│   ├── Azure Container Apps Environment
│   ├── Azure SQL Database Server
│   ├── Azure Container Registry
│   ├── Azure Service Bus Namespace
│   ├── Azure Application Insights
│   ├── Azure Log Analytics Workspace
│   ├── Azure Key Vault
│   └── Azure Storage Account
│
├── Resource Group: rg-kids-money-note-dev
│   └── (開発環境の同一構成)
│
└── Resource Group: rg-kids-money-note-shared
    ├── Azure Container Registry (共有)
    └── Azure DevOps/GitHub Actions 関連リソース
```

### 1.2 ネットワーク構成

```
Azure Virtual Network: vnet-kids-money-note
├── Subnet: snet-container-apps (10.0.1.0/24)
├── Subnet: snet-sql-database (10.0.2.0/24)
├── Subnet: snet-private-endpoints (10.0.3.0/24)
└── Subnet: snet-application-gateway (10.0.4.0/24)
```

## 2. Azure Container Apps

### 2.1 Container Apps Environment

```bicep
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-kids-money-note-${environment}'
  location: location
  properties: {
    vnetConfiguration: {
      infrastructureSubnetId: subnetContainerApps.id
      internal: false
    }
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
      {
        name: 'D4'
        workloadProfileType: 'D4'
        minimumCount: 1
        maximumCount: 3
      }
    ]
  }
}
```

### 2.2 各マイクロサービスのContainer App設定

#### User Service

```bicep
resource userServiceApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-user-service-${environment}'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/user-db-connection-string'
          identity: userAssignedIdentity.id
        }
      ]
      registries: [
        {
          server: '${containerRegistry.name}.azurecr.io'
          identity: userAssignedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'user-service'
          image: '${containerRegistry.name}.azurecr.io/user-service:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ConnectionStrings__UserDb'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
}
```

### 2.3 スケーリング設定

| サービス | 最小レプリカ | 最大レプリカ | スケール条件 |
|----------|-------------|-------------|-------------|
| User Service | 1 | 5 | HTTP requests > 10 concurrent |
| Account Service | 1 | 5 | HTTP requests > 10 concurrent |
| Transaction Service | 2 | 8 | HTTP requests > 15 concurrent |
| Goal Service | 1 | 3 | HTTP requests > 10 concurrent |
| Notification Service | 1 | 4 | Queue length > 20 messages |
| Report Service | 1 | 3 | HTTP requests > 5 concurrent |

### 2.4 リソース制限

```yaml
resources:
  cpu: 0.25 # CPU コア数
  memory: 0.5Gi # メモリ容量

# 高負荷サービス（Transaction Service）
resources:
  cpu: 0.5
  memory: 1.0Gi
```

## 3. Azure SQL Database

### 3.1 Azure SQL Server設定

```bicep
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-kids-money-note-${environment}'
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.3'
    publicNetworkAccess: 'Disabled' // プライベートエンドポイント経由のみ
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Microsoft Entra ID管理者設定
resource sqlServerAADAdmin 'Microsoft.Sql/servers/administrators@2023-08-01-preview' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: 'sql-admin-group@${tenantDomain}'
    sid: sqlAdminGroupObjectId
    tenantId: tenant().tenantId
  }
}
```

### 3.2 データベース設定

```bicep
// 各マイクロサービス用データベース
var databases = [
  { name: 'UserDb', service: 'user' }
  { name: 'AccountDb', service: 'account' }
  { name: 'TransactionDb', service: 'transaction' }
  { name: 'GoalDb', service: 'goal' }
  { name: 'NotificationDb', service: 'notification' }
  { name: 'ReportDb', service: 'report' }
]

resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-08-01-preview' = [for db in databases: {
  parent: sqlServer
  name: '${db.name}-${environment}'
  location: location
  sku: {
    name: environment == 'prod' ? 'S1' : 'Basic'
    tier: environment == 'prod' ? 'Standard' : 'Basic'
  }
  properties: {
    collation: 'Japanese_CI_AS'
    maxSizeBytes: environment == 'prod' ? 268435456000 : 2147483648 // 250GB : 2GB
    catalogCollation: 'Japanese_CI_AS'
    zoneRedundant: environment == 'prod' ? true : false
    readScale: environment == 'prod' ? 'Enabled' : 'Disabled'
    requestedBackupStorageRedundancy: 'GeoZone'
  }
}]
```

### 3.3 エラスティックプール（本番環境）

```bicep
resource elasticPool 'Microsoft.Sql/servers/elasticPools@2023-08-01-preview' = if (environment == 'prod') {
  parent: sqlServer
  name: 'pool-kids-money-note-prod'
  location: location
  sku: {
    name: 'StandardPool'
    tier: 'Standard'
    capacity: 200 // eDTU
  }
  properties: {
    perDatabaseSettings: {
      minCapacity: 0
      maxCapacity: 50
    }
    maxSizeBytes: 1073741824000 // 1TB
    zoneRedundant: true
  }
}
```

### 3.4 プライベートエンドポイント

```bicep
resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: 'pe-sql-kids-money-note-${environment}'
  location: location
  properties: {
    subnet: {
      id: subnetPrivateEndpoints.id
    }
    privateLinkServiceConnections: [
      {
        name: 'sql-connection'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: ['sqlServer']
        }
      }
    ]
  }
}
```

## 4. Azure Service Bus

### 4.1 Service Bus Namespace

```bicep
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2023-01-01-preview' = {
  name: 'sb-kids-money-note-${environment}'
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard' : 'Basic'
    tier: environment == 'prod' ? 'Standard' : 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.3'
    publicNetworkAccess: 'Disabled'
    disableLocalAuth: true // Azure AD認証のみ
  }
  identity: {
    type: 'SystemAssigned'
  }
}
```

### 4.2 Topics and Subscriptions

```bicep
// イベント用トピック
var topics = [
  {
    name: 'transaction-events'
    subscriptions: ['account-service', 'notification-service', 'report-service']
  }
  {
    name: 'goal-events'
    subscriptions: ['notification-service', 'report-service']
  }
  {
    name: 'user-events'
    subscriptions: ['account-service', 'goal-service']
  }
]

resource serviceBusTopics 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = [for topic in topics: {
  parent: serviceBusNamespace
  name: topic.name
  properties: {
    maxMessageSizeInKilobytes: 256
    defaultMessageTimeToLive: 'P14D' // 14日間
    enableBatchedOperations: true
    enablePartitioning: environment == 'prod' ? true : false
  }
}]

// 各サブスクリプション
resource serviceBusSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview' = [for (topic, topicIndex) in topics: for (subscription, subIndex) in topic.subscriptions: {
  parent: serviceBusTopics[topicIndex]
  name: subscription
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxDeliveryCount: 5
    enableBatchedOperations: true
  }
}]
```

## 5. Azure Application Insights

### 5.1 Application Insights設定

```bicep
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-kids-money-note-${environment}'
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

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-kids-money-note-${environment}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}
```

### 5.2 カスタムメトリクス設定

```bicep
// アラートルール
resource alertRules 'Microsoft.Insights/metricAlerts@2018-03-01' = [for rule in alertRules: {
  name: 'alert-${rule.name}-${environment}'
  location: 'global'
  properties: {
    description: rule.description
    severity: rule.severity
    enabled: true
    scopes: [
      applicationInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'metric1'
          metricName: rule.metricName
          operator: rule.operator
          threshold: rule.threshold
          timeAggregation: 'Average'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}]

var alertRules = [
  {
    name: 'high-response-time'
    description: 'High response time detected'
    severity: 2
    metricName: 'requests/duration'
    operator: 'GreaterThan'
    threshold: 2000 // 2秒
  }
  {
    name: 'high-error-rate'
    description: 'High error rate detected'
    severity: 1
    metricName: 'requests/failed'
    operator: 'GreaterThan'
    threshold: 10 // 10%
  }
  {
    name: 'low-availability'
    description: 'Low availability detected'
    severity: 0
    metricName: 'availabilityResults/availabilityPercentage'
    operator: 'LessThan'
    threshold: 99 // 99%
  }
]
```

## 6. Azure Key Vault

### 6.1 Key Vault設定

```bicep
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-kids-money-note-${environment}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
    }
  }
}

// マネージドIDにKey Vault Secrets Userロールを付与
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, userAssignedIdentity.id, 'Key Vault Secrets User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: userAssignedIdentity.properties.principalId
  }
}
```

### 6.2 シークレット管理

```bicep
// データベース接続文字列
resource dbConnectionSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [for db in databases: {
  parent: keyVault
  name: '${db.service}-db-connection-string'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${db.name}-${environment};Authentication=Active Directory Managed Identity;Encrypt=True;'
  }
}]

// Service Bus接続文字列
resource serviceBusConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'servicebus-connection-string'
  properties: {
    value: 'Endpoint=sb://${serviceBusNamespace.name}.servicebus.windows.net/;Authentication=Managed Identity'
  }
}
```

## 7. Azure Storage Account

### 7.1 Storage Account設定

```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'stkidsmoneynote${environment}${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_3'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // Azure AD認証のみ
    supportsHttpsTrafficOnly: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: subnetContainerApps.id
          action: 'Allow'
        }
      ]
    }
  }
}

// Blob Container for user avatars
resource avatarContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccount::blobService
  name: 'avatars'
  properties: {
    publicAccess: 'None'
  }
}
```

## 8. セキュリティ設定

### 8.1 ネットワークセキュリティ

```bicep
// Network Security Group
resource nsgContainerApps 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: 'nsg-container-apps-${environment}'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowHTTPS'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1000
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowHTTP'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1001
          direction: 'Inbound'
        }
      }
    ]
  }
}
```

### 8.2 Azure Policy適用

```bicep
// リソースタグ必須ポリシー
resource tagPolicy 'Microsoft.Authorization/policyAssignments@2024-04-01' = {
  name: 'require-tags-kids-money-note'
  scope: resourceGroup()
  properties: {
    policyDefinitionId: '/providers/Microsoft.Authorization/policyDefinitions/1e30110a-5ceb-460c-a204-c1c3969c6d62' // Require a tag on resources
    parameters: {
      tagName: {
        value: 'Environment'
      }
    }
  }
}

// リソース命名規則ポリシー
resource namingPolicy 'Microsoft.Authorization/policyAssignments@2024-04-01' = {
  name: 'enforce-naming-convention'
  scope: resourceGroup()
  properties: {
    policyDefinitionId: '/providers/Microsoft.Authorization/policyDefinitions/56a5ee18-2ae6-4810-86f7-18e39ce5629b' // Allowed resource types
    parameters: {
      listOfResourceTypesAllowed: {
        value: [
          'Microsoft.App/containerApps'
          'Microsoft.App/managedEnvironments'
          'Microsoft.Sql/servers'
          'Microsoft.Sql/servers/databases'
          'Microsoft.ServiceBus/namespaces'
          'Microsoft.Insights/components'
          'Microsoft.KeyVault/vaults'
          'Microsoft.Storage/storageAccounts'
        ]
      }
    }
  }
}
```

## 9. 監視・ログ設定

### 9.1 診断設定

```bicep
// Container Apps Environment診断設定
resource containerAppsEnvDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: containerAppsEnvironment
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'ContainerAppConsoleLogs'
        enabled: true
      }
      {
        category: 'ContainerAppSystemLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// SQL Database診断設定
resource sqlDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = [for (db, index) in databases: {
  scope: sqlDatabases[index]
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'SQLInsights'
        enabled: true
      }
      {
        category: 'AutomaticTuning'
        enabled: true
      }
      {
        category: 'QueryStoreRuntimeStatistics'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Basic'
        enabled: true
      }
    ]
  }
}]
```

### 9.2 カスタムダッシュボード

```bicep
resource dashboard 'Microsoft.Portal/dashboards@2020-09-01-preview' = {
  name: 'dashboard-kids-money-note-${environment}'
  location: location
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          {
            position: { x: 0, y: 0, rowSpan: 4, colSpan: 6 }
            metadata: {
              inputs: [
                {
                  name: 'ComponentId'
                  value: applicationInsights.id
                }
              ]
              type: 'Extension/AppInsightsExtension/PartType/AppMapGalPt'
            }
          }
          {
            position: { x: 6, y: 0, rowSpan: 4, colSpan: 6 }
            metadata: {
              inputs: [
                {
                  name: 'ComponentId'
                  value: applicationInsights.id
                }
              ]
              type: 'Extension/AppInsightsExtension/PartType/PerformanceCountersPart'
            }
          }
        ]
      }
    ]
  }
  tags: {
    Environment: environment
    Application: 'KidsMoneyNote'
  }
}
```

## 10. コスト最適化

### 10.1 リソース階層別コスト

| 環境 | 月額概算コスト (USD) | 主要コスト要素 |
|------|---------------------|----------------|
| Development | $200-300 | Basic SKU, 小規模インスタンス |
| Production | $800-1200 | Standard SKU, 冗長化構成 |

### 10.2 コスト削減策

#### 開発環境
- Container Apps: Consumption プランのみ使用
- SQL Database: Basic SKU
- Service Bus: Basic SKU
- 夜間・週末の自動停止

#### 本番環境
- Azure Reservations: 1年予約でコスト削減
- Spot Instances: 開発・テスト用ワークロード
- 自動スケーリングによるリソース最適化

## 11. 災害復旧 (DR)

### 11.1 DR戦略

```bicep
// セカンダリリージョン（本番環境のみ）
var primaryRegion = 'Japan East'
var secondaryRegion = 'Japan West'

// SQL Database Geo-Replication
resource sqlDatabaseFailoverGroup 'Microsoft.Sql/servers/failoverGroups@2023-08-01-preview' = if (environment == 'prod') {
  parent: sqlServer
  name: 'fg-kids-money-note-prod'
  properties: {
    readWriteEndpoint: {
      failoverPolicy: 'Automatic'
      failoverWithDataLossGracePeriodMinutes: 60
    }
    readOnlyEndpoint: {
      failoverPolicy: 'Enabled'
    }
    partnerServers: [
      {
        id: sqlServerSecondary.id
      }
    ]
    databases: [for db in databases: {
      id: sqlDatabases[db.name].id
    }]
  }
}
```

### 11.2 RTO/RPO目標

| サービス | RTO | RPO | 復旧方法 |
|----------|-----|-----|----------|
| Container Apps | 15分 | 0分 | 自動フェイルオーバー |
| SQL Database | 1時間 | 15分 | Geo-Failover |
| Service Bus | 30分 | 0分 | Multi-Region構成 |
| Storage Account | 1時間 | 15分 | GRS復旧 |

## 12. デプロイメント

### 12.1 Infrastructure as Code

```bash
# Bicep テンプレートのデプロイ
az deployment group create \
  --resource-group rg-kids-money-note-prod \
  --template-file main.bicep \
  --parameters environment=prod \
              sqlAdminPassword=$SQL_ADMIN_PASSWORD \
              location='Japan East'
```

### 12.2 環境別パラメータファイル

```json
// parameters.prod.json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment": {
      "value": "prod"
    },
    "location": {
      "value": "Japan East"
    },
    "skuName": {
      "value": "Standard"
    },
    "replicaCount": {
      "value": 2
    }
  }
}
```