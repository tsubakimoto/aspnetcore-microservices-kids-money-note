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
- Azure サブスクリプション
- Visual Studio 2022 (v17.8以降) または Visual Studio Code
- Docker Desktop（開発環境用）
- Azure CLI

### セットアップ手順

1. **リポジトリのクローン**
   ```bash
   git clone https://github.com/tsubakimoto/aspnetcore-microservices-kids-money-note.git
   cd aspnetcore-microservices-kids-money-note
   ```

2. **環境変数設定**
   ```bash
   cp .env.example .env
   # .envファイルを編集して必要な設定値を入力
   ```

3. **Docker Composeでローカル環境起動**
   ```bash
   docker-compose up -d
   ```

4. **データベースマイグレーション実行**
   ```bash
   dotnet run --project src/Services/UserService/UserService.API -- --migrate
   dotnet run --project src/Services/AccountService/AccountService.API -- --migrate
   dotnet run --project src/Services/TransactionService/TransactionService.API -- --migrate
   ```

5. **初期データの投入**
   ```bash
   dotnet run --project src/Services/UserService/UserService.API -- --seed
   dotnet run --project src/Services/TransactionService/TransactionService.API -- --seed
   ```

詳細な開発・デプロイ手順については、[開発・デプロイメント設計書](./design-docs/07-development-deployment.md) をご参照ください。

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