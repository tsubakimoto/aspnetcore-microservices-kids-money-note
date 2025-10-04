# システムアーキテクチャ設計書

## 1. プロジェクト概要

### 1.1 プロジェクト名
Kids Money Note - こども用お小遣い管理アプリケーション

### 1.2 目的
小学生がお小遣いの管理を学習し、親が子どもの金銭管理状況を把握できるシステムを提供する。

### 1.3 対象ユーザー
- **メインユーザー**: 小学生（6-12歳）
- **セカンダリユーザー**: 保護者

### 1.4 主要機能
- お小遣い収入の記録
- 支出の記録・分類
- 貯金目標の設定
- 親への報告・通知機能
- 簡単な家計簿機能

## 2. システムアーキテクチャ

### 2.1 全体アーキテクチャ図

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend Layer                       │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Kids Web UI   │  │  Parent Web UI  │  │  Mobile App     │ │
│  │   (Blazor)      │  │   (Blazor)      │  │  (Future)       │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      API Gateway Layer                     │
├─────────────────────────────────────────────────────────────┤
│                    Azure Application Gateway               │
│                         (YARP)                             │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                   Microservices Layer                      │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────┐ │
│ │   Account   │ │ Transaction │ │    Goal     │ │   User   │ │
│ │  Service    │ │   Service   │ │  Service    │ │ Service  │ │
│ └─────────────┘ └─────────────┘ └─────────────┘ └──────────┘ │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐              │
│ │Notification │ │   Report    │ │   Auth      │              │
│ │  Service    │ │  Service    │ │  Service    │              │
│ └─────────────┘ └─────────────┘ └─────────────┘              │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      Data Layer                            │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────┐ │
│ │ User Store  │ │ Transaction │ │ Goal Store  │ │  Auth    │ │
│ │(Azure SQL)  │ │Store(Azure  │ │(Azure SQL)  │ │ Store    │ │
│ │             │ │     SQL)    │ │             │ │(Azure AD)│ │
│ └─────────────┘ └─────────────┘ └─────────────┘ └──────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 マイクロサービス構成

#### 2.2.1 User Service（ユーザー管理サービス）
- **責任**: ユーザー情報の管理、プロファイル管理
- **技術**: ASP.NET Core Web API
- **データベース**: Azure SQL Database

#### 2.2.2 Account Service（口座管理サービス）
- **責任**: お小遣い口座の残高管理
- **技術**: ASP.NET Core Web API
- **データベース**: Azure SQL Database

#### 2.2.3 Transaction Service（取引管理サービス）
- **責任**: 収入・支出の記録と管理
- **技術**: ASP.NET Core Web API
- **データベース**: Azure SQL Database

#### 2.2.4 Goal Service（目標管理サービス）
- **責任**: 貯金目標の設定と進捗管理
- **技術**: ASP.NET Core Web API
- **データベース**: Azure SQL Database

#### 2.2.5 Notification Service（通知サービス）
- **責任**: 親への通知、リマインダー
- **技術**: ASP.NET Core Web API + Azure Service Bus
- **外部サービス**: Azure Communication Services

#### 2.2.6 Report Service（レポートサービス）
- **責任**: 使用状況レポート、統計情報
- **技術**: ASP.NET Core Web API
- **データベース**: Azure SQL Database

#### 2.2.7 Auth Service（認証サービス）
- **責任**: 認証・認可
- **技術**: Microsoft Entra ID
- **機能**: OAuth 2.0, OpenID Connect

## 3. 技術スタック

### 3.1 開発技術
- **フレームワーク**: ASP.NET Core 9.0
- **言語**: C# 13
- **UI**: Blazor Server/WebAssembly
- **API**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 9.0

### 3.2 インフラストラクチャ
- **クラウド**: Microsoft Azure
- **コンテナ**: Azure Container Apps
- **データベース**: Azure SQL Database
- **認証**: Microsoft Entra ID
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **メッセージング**: Azure Service Bus
- **ストレージ**: Azure Blob Storage
- **監視**: Azure Application Insights
- **ログ**: Azure Log Analytics

### 3.3 開発・デプロイ
- **CI/CD**: GitHub Actions
- **コンテナレジストリ**: Azure Container Registry
- **Infrastructure as Code**: Azure Bicep
- **バージョン管理**: Git (GitHub)

## 4. 非機能要件

### 4.1 パフォーマンス
- **レスポンス時間**: 2秒以内（95%tile）
- **スループット**: 100 req/sec（ピーク時）
- **可用性**: 99.9%

### 4.2 スケーラビリティ
- **水平スケーリング**: Azure Container Apps Auto Scaling
- **データベース**: Azure SQL Database エラスティックプール

### 4.3 セキュリティ
- **認証**: Microsoft Entra ID
- **データ暗号化**: TLS 1.3 (転送中), AES-256 (保存時)
- **アクセス制御**: RBAC (Role-Based Access Control)

### 4.4 監視・ログ
- **アプリケーション監視**: Azure Application Insights
- **インフラ監視**: Azure Monitor
- **ログ集約**: Azure Log Analytics
- **アラート**: Azure Alerts

## 5. デプロイメント戦略

### 5.1 環境構成
- **開発環境**: ローカル開発環境 + Azure Dev環境
- **ステージング環境**: Azure Container Apps (Dev リソースグループ)
- **本番環境**: Azure Container Apps (Prod リソースグループ)

### 5.2 デプロイメントパターン
- **ブルーグリーンデプロイメント**: 本番環境
- **カナリアデプロイメント**: 重要な機能更新時
- **ローリングアップデート**: 通常の機能更新

## 6. データフロー

### 6.1 典型的なデータフロー例（お小遣い記録）

```
1. Kids Web UI → API Gateway → Transaction Service
2. Transaction Service → Account Service (残高更新)
3. Transaction Service → Notification Service (親への通知)
4. Notification Service → Azure Communication Services → 親のメール/SMS
```

### 6.2 イベント駆動アーキテクチャ
- **イベント**: Azure Service Bus Topics
- **パターン**: Publish-Subscribe
- **用途**: マイクロサービス間の疎結合通信

## 7. 将来の拡張性

### 7.1 予定されている機能拡張
- モバイルアプリ（iOS/Android）
- 家族間の送金機能
- ポイント・リワードシステム
- AI による支出パターン分析

### 7.2 技術的拡張可能性
- マルチテナント対応
- 国際化（多言語対応）
- サードパーティAPI連携
- Machine Learning統合