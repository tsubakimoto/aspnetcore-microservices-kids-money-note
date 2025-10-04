# Kids Money Note - こども用お小遣い管理アプリケーション

Kids Money Noteは、ASP.NET Coreマイクロサービスアーキテクチャを使用したこども用のお小遣い管理アプリケーションです。小学生が自分のお小遣いを管理し、親が子どもの金銭管理状況を把握できるシステムを提供します。

## 🎯 主要機能

- **収支管理**: お小遣いの収入・支出を簡単に記録
- **目標設定**: 貯金目標の設定と進捗管理
- **親への通知**: 子どもの金銭管理状況を親にリアルタイム通知
- **レポート機能**: 月次・年次の使用状況レポート
- **シンプルUI**: 小学生でも使いやすい直感的なインターフェース

## 🏗️ システム構成

このアプリケーションは以下のマイクロサービスで構成されています：

- **User Service**: ユーザー管理（子ども・親）
- **Account Service**: 口座・残高管理
- **Transaction Service**: 取引記録・管理
- **Goal Service**: 貯金目標管理
- **Notification Service**: 通知・アラート機能
- **Report Service**: レポート生成・分析

## 📋 設計書

詳細な設計書は [`/design-docs`](./design-docs/) ディレクトリに格納されています：

- [設計書一覧](./design-docs/README.md)
- [システムアーキテクチャ設計書](./design-docs/01-system-architecture.md)
- [マイクロサービス設計書](./design-docs/02-microservices-design.md)
- [データベース設計書](./design-docs/03-database-design.md)
- [Azure インフラストラクチャ設計書](./design-docs/04-azure-infrastructure.md)
- [セキュリティ設計書](./design-docs/05-security-design.md)
- [API仕様書](./design-docs/06-api-specifications.md)
- [開発・デプロイメント設計書](./design-docs/07-development-deployment.md)

## 🛠️ 技術スタック

- **フレームワーク**: ASP.NET Core 9.0
- **言語**: C# 13
- **データベース**: Azure SQL Database (SQL Server 2025)
- **認証**: Microsoft Entra ID
- **コンテナ**: Azure Container Apps
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **メッセージング**: Azure Service Bus
- **監視**: Azure Application Insights
- **フロントエンド**: Blazor Server/WebAssembly
- **Infrastructure as Code**: Azure Bicep

## 🚀 開発環境構築

### 前提条件
- .NET 9.0 SDK
- Docker Desktop
- Azure CLI（本番デプロイ用）
- Visual Studio 2022 (v17.8以降) または Visual Studio Code

### セットアップ手順

1. **リポジトリのクローン**
   ```bash
   git clone https://github.com/tsubakimoto/aspnetcore-microservices-kids-money-note.git
   cd aspnetcore-microservices-kids-money-note
   ```

2. **ソリューションのビルド**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **User Serviceの単体実行（開発・テスト用）**
   ```bash
   cd src/Services/UserService/UserService.API
   dotnet run
   ```
   
   起動後、http://localhost:5247 でSwagger UIにアクセスできます。

4. **Docker Composeでマルチサービス環境起動**
   ```bash
   # 全サービスをビルドして起動
   docker-compose up -d --build
   
   # ログの確認
   docker-compose logs -f
   
   # 停止
   docker-compose down
   ```

5. **サービスエンドポイント（Docker Compose環境）**
   - API Gateway: http://localhost:8000
   - User Service: http://localhost:8001
   - Account Service: http://localhost:8002
   - Transaction Service: http://localhost:8003
   - Goal Service: http://localhost:8004
   - Kids Web App: http://localhost:8080
   - Parent Web App: http://localhost:8081
   - RabbitMQ Management: http://localhost:15672 (admin/admin)

### テスト実行

```bash
# 全てのテストを実行
dotnet test

# 特定のプロジェクトのテストを実行
dotnet test tests/IntegrationTests/

# カバレッジレポート付きテスト実行
dotnet test --collect:"XPlat Code Coverage"
```

詳細な開発・デプロイ手順については、[開発・デプロイメント設計書](./design-docs/07-development-deployment.md) をご参照ください。

## 📊 実装状況

### 完了済み ✅
- **プロジェクト構造**: 全マイクロサービスのプロジェクト構成完了
- **User Service**: 完全実装（Domain、Infrastructure、API、テスト済み）
- **共通ライブラリ**: API レスポンス形式、ページネーション等
- **Docker構成**: Docker Compose環境構築完了
- **CI/CDパイプライン**: GitHub Actions ワークフロー基盤
- **インフラストラクチャ**: Azure Bicep テンプレート基盤

### 実装中/予定 🚧
- **Account Service**: ドメインモデルと基本API
- **Transaction Service**: 取引管理とカテゴリマスタ
- **Goal Service**: 目標設定と進捗管理
- **Notification Service**: イベント駆動通知システム
- **Report Service**: レポート生成とデータ分析
- **API Gateway**: YARP を使用したリバースプロキシ
- **Blazor Web Apps**: Kids App と Parent App の実装
- **Event Bus**: Azure Service Bus を使用したメッセージング
- **認証・認可**: Microsoft Entra ID 統合

### アーキテクチャ特徴 🏗️
- **クリーンアーキテクチャ**: Domain-Infrastructure-API の3層構成
- **マイクロサービス**: 独立したデータベースとビジネス境界
- **イベント駆動**: Azure Service Bus を使用した非同期メッセージング
- **コンテナ化**: Docker コンテナでの統一された実行環境
- **クラウドネイティブ**: Azure Container Apps でのサーバーレス実行

## 🔧 アーキテクチャ概要

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
│ ┌─────────────┐ ┌─────────────┐                              │
│ │Notification │ │   Report    │                              │
│ │  Service    │ │  Service    │                              │
│ └─────────────┘ └─────────────┘                              │
└─────────────────────────────────────────────────────────────┘
```

## 📝 ライセンス

このプロジェクトは、MIT ライセンスの下で公開されています。詳細については、[LICENSE](LICENSE) ファイルをご覧ください。

## 🤝 コントリビューション

コントリビューションを歓迎します！プルリクエストやイシューの報告を通じて、プロジェクトの改善にご協力ください。

## 📞 サポート

質問やサポートが必要な場合は、GitHubのIssuesを通じてお気軽にお問い合わせください。