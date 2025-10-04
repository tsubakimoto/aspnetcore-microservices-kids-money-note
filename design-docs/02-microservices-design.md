# マイクロサービス設計書

## 1. マイクロサービス一覧

### 1.1 サービス構成概要

| サービス名 | 責任範囲 | データストア | 技術スタック |
|------------|----------|-------------|-------------|
| User Service | ユーザー管理 | Azure SQL Database | ASP.NET Core Web API |
| Account Service | 口座・残高管理 | Azure SQL Database | ASP.NET Core Web API |
| Transaction Service | 取引管理 | Azure SQL Database | ASP.NET Core Web API |
| Goal Service | 目標管理 | Azure SQL Database | ASP.NET Core Web API |
| Notification Service | 通知管理 | Azure Service Bus | ASP.NET Core Web API |
| Report Service | レポート生成 | Azure SQL Database | ASP.NET Core Web API |
| Auth Service | 認証・認可 | Microsoft Entra ID | Microsoft Identity Platform |

## 2. User Service（ユーザー管理サービス）

### 2.1 概要
- **目的**: ユーザー（子ども・親）の基本情報管理
- **Port**: 8001
- **Database**: UserDb

### 2.2 API エンドポイント

#### 2.2.1 ユーザー管理

```http
# ユーザー情報取得
GET /api/users/{userId}

# ユーザー登録
POST /api/users
Content-Type: application/json
{
  "name": "田中太郎",
  "email": "tanaka@example.com",
  "role": "Child", // Child, Parent
  "birthDate": "2015-04-01",
  "parentId": "guid" // 子どもの場合
}

# ユーザー情報更新
PUT /api/users/{userId}

# 子どもリスト取得（親用）
GET /api/users/parent/{parentId}/children
```

### 2.3 データモデル

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime BirthDate { get; set; }
    public Guid? ParentId { get; set; } // 子どもの場合の親ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum UserRole
{
    Child,
    Parent
}
```

## 3. Account Service（口座管理サービス）

### 3.1 概要
- **目的**: お小遣い口座の残高管理
- **Port**: 8002
- **Database**: AccountDb

### 3.2 API エンドポイント

```http
# 口座情報取得
GET /api/accounts/{userId}

# 口座作成
POST /api/accounts
{
  "userId": "guid",
  "accountName": "太郎のお小遣い口座",
  "initialBalance": 1000
}

# 残高照会
GET /api/accounts/{accountId}/balance

# 残高更新（内部API）
PUT /api/accounts/{accountId}/balance
{
  "amount": 500,
  "operation": "Add" // Add, Subtract
}
```

### 3.3 データモデル

```csharp
public class Account
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## 4. Transaction Service（取引管理サービス）

### 4.1 概要
- **目的**: 収入・支出の記録と管理
- **Port**: 8003
- **Database**: TransactionDb

### 4.2 API エンドポイント

```http
# 取引記録
POST /api/transactions
{
  "userId": "guid",
  "type": "Income", // Income, Expense
  "amount": 500,
  "category": "お手伝い",
  "description": "お皿洗いのお手伝い",
  "date": "2024-12-14"
}

# 取引履歴取得
GET /api/transactions/user/{userId}?page=1&size=10&startDate=2024-12-01&endDate=2024-12-31

# カテゴリ別集計
GET /api/transactions/user/{userId}/summary?period=month&year=2024&month=12

# 取引削除
DELETE /api/transactions/{transactionId}
```

### 4.3 データモデル

```csharp
public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum TransactionType
{
    Income,
    Expense
}

public class TransactionSummary
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}
```

### 4.4 カテゴリマスタ

#### 収入カテゴリ
- お手伝い
- お年玉
- 誕生日プレゼント
- テストの頑張り
- その他

#### 支出カテゴリ
- お菓子
- おもちゃ
- 本・雑誌
- 文房具
- 貯金
- その他

## 5. Goal Service（目標管理サービス）

### 5.1 概要
- **目的**: 貯金目標の設定と進捗管理
- **Port**: 8004
- **Database**: GoalDb

### 5.2 API エンドポイント

```http
# 目標作成
POST /api/goals
{
  "userId": "guid",
  "title": "新しいゲーム",
  "targetAmount": 5000,
  "currentAmount": 1000,
  "targetDate": "2025-03-01",
  "description": "欲しかったゲームを買うため"
}

# 目標一覧取得
GET /api/goals/user/{userId}

# 目標進捗更新
PUT /api/goals/{goalId}/progress
{
  "amount": 500
}

# 目標達成
PUT /api/goals/{goalId}/complete
```

### 5.3 データモデル

```csharp
public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime TargetDate { get; set; }
    public GoalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum GoalStatus
{
    Active,
    Completed,
    Cancelled
}
```

## 6. Notification Service（通知サービス）

### 6.1 概要
- **目的**: 親への通知、子どもへのリマインダー
- **Port**: 8005
- **Technology**: Azure Service Bus + Azure Communication Services

### 6.2 API エンドポイント

```http
# 通知送信
POST /api/notifications
{
  "userId": "guid",
  "type": "TransactionAlert", // TransactionAlert, GoalProgress, MonthlyReport
  "title": "取引通知",
  "message": "太郎くんがお菓子を100円で購入しました",
  "recipientId": "guid" // 通知先（親）
}

# 通知履歴取得
GET /api/notifications/user/{userId}?page=1&size=10

# 通知設定更新
PUT /api/notifications/settings/{userId}
{
  "emailNotifications": true,
  "smsNotifications": false,
  "pushNotifications": true
}
```

### 6.3 データモデル

```csharp
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public enum NotificationType
{
    TransactionAlert,
    GoalProgress,
    MonthlyReport,
    Reminder
}

public class NotificationSettings
{
    public Guid UserId { get; set; }
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public bool PushNotifications { get; set; }
}
```

## 7. Report Service（レポートサービス）

### 7.1 概要
- **目的**: 使用状況レポート、統計情報の生成
- **Port**: 8006
- **Database**: 各サービスのデータを集約

### 7.2 API エンドポイント

```http
# 月次レポート取得
GET /api/reports/monthly/{userId}?year=2024&month=12

# 年次レポート取得
GET /api/reports/yearly/{userId}?year=2024

# カテゴリ別分析
GET /api/reports/category-analysis/{userId}?period=6months

# 目標達成率レポート
GET /api/reports/goal-achievement/{userId}
```

### 7.3 データモデル

```csharp
public class MonthlyReport
{
    public Guid UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount { get; set; }
    public decimal EndingBalance { get; set; }
    public List<CategorySummary> IncomeByCategory { get; set; } = new();
    public List<CategorySummary> ExpenseByCategory { get; set; } = new();
    public List<GoalProgress> GoalProgresses { get; set; } = new();
}

public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}
```

## 8. サービス間通信

### 8.1 同期通信
- **HTTP/REST**: サービス間の直接呼び出し
- **認証**: Service-to-Service authentication

### 8.2 非同期通信
- **Azure Service Bus**: イベント駆動通信
- **イベント例**:
  - TransactionCreated → Account Service, Notification Service
  - GoalCompleted → Notification Service
  - BalanceChanged → Report Service

### 8.3 イベント定義

```csharp
public class TransactionCreatedEvent
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public class GoalCompletedEvent
{
    public Guid GoalId { get; set; }
    public Guid UserId { get; set; }
    public string GoalTitle { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
```

## 9. エラーハンドリング

### 9.1 共通エラーレスポンス

```csharp
public class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
```

### 9.2 HTTPステータスコード
- **200**: 成功
- **201**: 作成成功
- **400**: 不正なリクエスト
- **401**: 認証エラー
- **403**: 認可エラー
- **404**: リソースが見つからない
- **500**: サーバーエラー

## 10. ヘルスチェック

### 10.1 ヘルスチェックエンドポイント

```http
# 基本ヘルスチェック
GET /health

# 詳細ヘルスチェック
GET /health/ready

# データベース接続チェック
GET /health/db
```

### 10.2 ヘルスチェックレスポンス

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0098765"
    },
    "servicebus": {
      "status": "Healthy", 
      "duration": "00:00:00.0024691"
    }
  }
}
```