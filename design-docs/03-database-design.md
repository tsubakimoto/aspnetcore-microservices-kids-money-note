# データベース設計書

## 1. データベース概要

### 1.1 データベース構成
- **データベースエンジン**: Azure SQL Database
- **バージョン**: SQL Server 2025 compatible
- **接続方式**: Entity Framework Core 9.0
- **認証**: Azure AD統合認証

### 1.2 データベース分散設計
マイクロサービス毎に独立したデータベースを配置（Database per Service パターン）

| サービス | データベース名 | 主要テーブル |
|----------|---------------|-------------|
| User Service | UserDb | Users, UserProfiles |
| Account Service | AccountDb | Accounts, BalanceHistory |
| Transaction Service | TransactionDb | Transactions, Categories |
| Goal Service | GoalDb | Goals, GoalProgress |
| Notification Service | NotificationDb | Notifications, NotificationSettings |
| Report Service | ReportDb | ReportCache, Analytics |

## 2. User Service データベース (UserDb)

### 2.1 Users テーブル

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Role INT NOT NULL, -- 0: Child, 1: Parent
    BirthDate DATE NOT NULL,
    ParentId UNIQUEIDENTIFIER NULL, -- 子どもの場合の親ID
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Users_Parent FOREIGN KEY (ParentId) REFERENCES Users(Id),
    CONSTRAINT CK_Users_Role CHECK (Role IN (0, 1)),
    CONSTRAINT CK_Users_ChildHasParent CHECK (
        (Role = 1 AND ParentId IS NULL) OR 
        (Role = 0 AND ParentId IS NOT NULL)
    )
);

CREATE INDEX IX_Users_ParentId ON Users(ParentId);
CREATE INDEX IX_Users_Email ON Users(Email);
```

### 2.2 UserProfiles テーブル

```sql
CREATE TABLE UserProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    AvatarUrl NVARCHAR(500) NULL,
    Theme NVARCHAR(20) NOT NULL DEFAULT 'default',
    Language NVARCHAR(10) NOT NULL DEFAULT 'ja-JP',
    TimeZone NVARCHAR(50) NOT NULL DEFAULT 'Asia/Tokyo',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_UserProfiles_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_UserProfiles_UserId ON UserProfiles(UserId);
```

## 3. Account Service データベース (AccountDb)

### 3.1 Accounts テーブル

```sql
CREATE TABLE Accounts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    AccountName NVARCHAR(100) NOT NULL,
    Balance DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_Accounts_Balance_NonNegative CHECK (Balance >= 0.00)
);

CREATE INDEX IX_Accounts_UserId ON Accounts(UserId);
```

### 3.2 BalanceHistory テーブル

```sql
CREATE TABLE BalanceHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AccountId UNIQUEIDENTIFIER NOT NULL,
    PreviousBalance DECIMAL(10,2) NOT NULL,
    NewBalance DECIMAL(10,2) NOT NULL,
    ChangeAmount DECIMAL(10,2) NOT NULL,
    ChangeType INT NOT NULL, -- 0: Increase, 1: Decrease
    TransactionId UNIQUEIDENTIFIER NULL, -- 関連する取引ID
    Reason NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_BalanceHistory_Account FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,
    CONSTRAINT CK_BalanceHistory_ChangeType CHECK (ChangeType IN (0, 1))
);

CREATE INDEX IX_BalanceHistory_AccountId ON BalanceHistory(AccountId);
CREATE INDEX IX_BalanceHistory_CreatedAt ON BalanceHistory(CreatedAt);
```

## 4. Transaction Service データベース (TransactionDb)

### 4.1 Categories テーブル

```sql
CREATE TABLE Categories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(50) NOT NULL,
    Type INT NOT NULL, -- 0: Income, 1: Expense
    IconName NVARCHAR(50) NULL,
    Color NVARCHAR(7) NULL, -- HEX color code
    IsDefault BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- デフォルトカテゴリーの挿入
INSERT INTO Categories (Name, Type, IconName, Color, IsDefault) VALUES
('お手伝い', 0, 'help', '#4CAF50', 1),
('お年玉', 0, 'gift', '#2196F3', 1),
('誕生日プレゼント', 0, 'cake', '#FF9800', 1),
('テストの頑張り', 0, 'school', '#9C27B0', 1),
('その他収入', 0, 'more', '#607D8B', 1),
('お菓子', 1, 'candy', '#F44336', 1),
('おもちゃ', 1, 'toys', '#E91E63', 1),
('本・雑誌', 1, 'book', '#3F51B5', 1),
('文房具', 1, 'edit', '#009688', 1),
('貯金', 1, 'savings', '#795548', 1),
('その他支出', 1, 'more', '#9E9E9E', 1);
```

### 4.2 Transactions テーブル

```sql
CREATE TABLE Transactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    Type INT NOT NULL, -- 0: Income, 1: Expense
    Amount DECIMAL(10,2) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    TransactionDate DATE NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Transactions_Category FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT CK_Transactions_Type CHECK (Type IN (0, 1)),
    CONSTRAINT CK_Transactions_Amount_Positive CHECK (Amount > 0.00)
);

CREATE INDEX IX_Transactions_UserId ON Transactions(UserId);
CREATE INDEX IX_Transactions_TransactionDate ON Transactions(TransactionDate);
CREATE INDEX IX_Transactions_Type ON Transactions(Type);
CREATE INDEX IX_Transactions_CategoryId ON Transactions(CategoryId);
```

## 5. Goal Service データベース (GoalDb)

### 5.1 Goals テーブル

```sql
CREATE TABLE Goals (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    TargetAmount DECIMAL(10,2) NOT NULL,
    CurrentAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    TargetDate DATE NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0: Active, 1: Completed, 2: Cancelled
    Priority INT NOT NULL DEFAULT 1, -- 1: Low, 2: Medium, 3: High
    ImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,
    
    CONSTRAINT CK_Goals_TargetAmount_Positive CHECK (TargetAmount > 0.00),
    CONSTRAINT CK_Goals_CurrentAmount_NonNegative CHECK (CurrentAmount >= 0.00),
    CONSTRAINT CK_Goals_CurrentAmount_LTE_Target CHECK (CurrentAmount <= TargetAmount),
    CONSTRAINT CK_Goals_Status CHECK (Status IN (0, 1, 2)),
    CONSTRAINT CK_Goals_Priority CHECK (Priority IN (1, 2, 3)),
    CONSTRAINT CK_Goals_TargetDate_Future CHECK (TargetDate >= CAST(GETDATE() AS DATE))
);

CREATE INDEX IX_Goals_UserId ON Goals(UserId);
CREATE INDEX IX_Goals_Status ON Goals(Status);
CREATE INDEX IX_Goals_TargetDate ON Goals(TargetDate);
```

### 5.2 GoalProgress テーブル

```sql
CREATE TABLE GoalProgress (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GoalId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    PreviousAmount DECIMAL(10,2) NOT NULL,
    NewAmount DECIMAL(10,2) NOT NULL,
    Note NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_GoalProgress_Goal FOREIGN KEY (GoalId) REFERENCES Goals(Id) ON DELETE CASCADE,
    CONSTRAINT CK_GoalProgress_Amount_Positive CHECK (Amount > 0.00)
);

CREATE INDEX IX_GoalProgress_GoalId ON GoalProgress(GoalId);
CREATE INDEX IX_GoalProgress_CreatedAt ON GoalProgress(CreatedAt);
```

## 6. Notification Service データベース (NotificationDb)

### 6.1 Notifications テーブル

```sql
CREATE TABLE Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL, -- 通知の発生者
    RecipientId UNIQUEIDENTIFIER NOT NULL, -- 通知の受信者
    Type INT NOT NULL, -- 0: TransactionAlert, 1: GoalProgress, 2: MonthlyReport, 3: Reminder
    Title NVARCHAR(100) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    ReadAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_Notifications_Type CHECK (Type IN (0, 1, 2, 3))
);

CREATE INDEX IX_Notifications_RecipientId ON Notifications(RecipientId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);
CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);
```

### 6.2 NotificationSettings テーブル

```sql
CREATE TABLE NotificationSettings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    EmailNotifications BIT NOT NULL DEFAULT 1,
    SmsNotifications BIT NOT NULL DEFAULT 0,
    PushNotifications BIT NOT NULL DEFAULT 1,
    TransactionAlerts BIT NOT NULL DEFAULT 1,
    GoalReminders BIT NOT NULL DEFAULT 1,
    MonthlyReports BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_NotificationSettings_UserId ON NotificationSettings(UserId);
```

## 7. Report Service データベース (ReportDb)

### 7.1 ReportCache テーブル（レポートキャッシュ）

```sql
CREATE TABLE ReportCache (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ReportType INT NOT NULL, -- 0: Monthly, 1: Yearly, 2: Category, 3: Goal
    ReportPeriod NVARCHAR(20) NOT NULL, -- 'YYYY-MM' or 'YYYY' or 'YYYY-MM-DD'
    ReportData NVARCHAR(MAX) NOT NULL, -- JSON形式のレポートデータ
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    
    CONSTRAINT CK_ReportCache_Type CHECK (ReportType IN (0, 1, 2, 3))
);

CREATE INDEX IX_ReportCache_UserId_Type_Period ON ReportCache(UserId, ReportType, ReportPeriod);
CREATE INDEX IX_ReportCache_ExpiresAt ON ReportCache(ExpiresAt);
```

### 7.2 Analytics テーブル（分析データ）

```sql
CREATE TABLE Analytics (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    MetricType INT NOT NULL, -- 0: DailyExpense, 1: CategoryTrend, 2: GoalProgress, 3: SavingsRate
    MetricDate DATE NOT NULL,
    MetricValue DECIMAL(10,2) NOT NULL,
    MetricMetadata NVARCHAR(MAX) NULL, -- JSON形式のメタデータ
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_Analytics_MetricType CHECK (MetricType IN (0, 1, 2, 3))
);

CREATE INDEX IX_Analytics_UserId_MetricType ON Analytics(UserId, MetricType);
CREATE INDEX IX_Analytics_MetricDate ON Analytics(MetricDate);
```

## 8. データベース共通設計原則

### 8.1 命名規則
- **テーブル名**: パスカルケース（例: Users, Transactions）
- **カラム名**: パスカルケース（例: UserId, CreatedAt）
- **インデックス名**: IX_{テーブル名}_{カラム名}
- **外部キー制約名**: FK_{テーブル名}_{参照テーブル名}
- **チェック制約名**: CK_{テーブル名}_{制約内容}

### 8.2 共通カラム
全てのテーブルに以下のカラムを含める：
- **Id**: UNIQUEIDENTIFIER PRIMARY KEY
- **CreatedAt**: DATETIME2 NOT NULL DEFAULT GETUTCDATE()
- **UpdatedAt**: DATETIME2 NOT NULL DEFAULT GETUTCDATE() （更新可能テーブルのみ）

### 8.3 データ型統一
- **ID**: UNIQUEIDENTIFIER
- **文字列**: NVARCHAR
- **金額**: DECIMAL(10,2)
- **日時**: DATETIME2
- **フラグ**: BIT

## 9. パフォーマンス最適化

### 9.1 インデックス戦略
- **主キー**: 自動的にクラスター化インデックス
- **外部キー**: 非クラスター化インデックス
- **検索頻度の高いカラム**: 複合インデックス
- **ページング用**: OrderBy対象カラムにインデックス

### 9.2 パーティショニング（将来の拡張）
- **Transactions テーブル**: 月別パーティション
- **BalanceHistory テーブル**: 月別パーティション
- **Analytics テーブル**: 年別パーティション

## 10. バックアップ・復旧戦略

### 10.1 バックアップ設定
- **完全バックアップ**: 週次
- **差分バックアップ**: 日次
- **ログバックアップ**: 15分間隔
- **保持期間**: 30日間

### 10.2 災害復旧
- **RTO**: 4時間以内
- **RPO**: 15分以内
- **Azure SQL Database**: Geo-redundant backup
- **レプリケーション**: 異なるリージョンへのRead Replica

## 11. セキュリティ設計

### 11.1 データ暗号化
- **保存時暗号化**: Transparent Data Encryption (TDE)
- **転送時暗号化**: TLS 1.3
- **Always Encrypted**: 機密データ（将来的な拡張）

### 11.2 アクセス制御
- **Azure AD統合**: サービス認証
- **RBAC**: ロールベースアクセス制御
- **Row Level Security**: ユーザーデータの分離

### 11.3 監査
- **Azure SQL Auditing**: 全データベース操作の記録
- **ログ保持**: 90日間
- **監視**: Azure Monitor統合

## 12. Entity Framework Core 設定

### 12.1 DbContext設定例

```csharp
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users テーブル設定
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ParentId);
            
            // 自己参照外部キー
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // UserProfiles テーブル設定
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Theme).HasMaxLength(20).HasDefaultValue("default");
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("ja-JP");
        });
    }
}
```

### 12.2 接続文字列設定

```json
{
  "ConnectionStrings": {
    "UserDb": "Server=tcp:{server}.database.windows.net,1433;Database=UserDb;Authentication=Active Directory Integrated;Encrypt=True;"
  }
}
```