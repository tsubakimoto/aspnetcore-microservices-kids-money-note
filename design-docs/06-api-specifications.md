# API仕様書

## 1. API概要

### 1.1 基本情報
- **APIバージョン**: v1
- **ベースURL**: `https://api.kidsmoneynoteweb.azurecontainerapps.io/api/v1`
- **認証方式**: JWT Bearer Token (Microsoft Entra ID)
- **データ形式**: JSON
- **文字エンコーディング**: UTF-8

### 1.2 共通仕様

#### 1.2.1 HTTPヘッダー

```http
# 必須ヘッダー
Authorization: Bearer {jwt_token}
Content-Type: application/json
Accept: application/json

# 推奨ヘッダー
X-Request-ID: {unique_request_id}
User-Agent: KidsMoneyNote-Web/1.0
```

#### 1.2.2 共通レスポンス形式

```json
// 成功レスポンス
{
  "data": {}, // 実際のデータ
  "success": true,
  "message": "Success",
  "timestamp": "2024-12-14T10:30:00Z",
  "requestId": "req_123456789"
}

// エラーレスポンス
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "入力データが不正です",
    "details": [
      {
        "field": "amount",
        "message": "金額は0より大きい値を入力してください"
      }
    ]
  },
  "success": false,
  "timestamp": "2024-12-14T10:30:00Z",
  "requestId": "req_123456789"
}
```

#### 1.2.3 ページネーション

```json
// ページネーション付きレスポンス
{
  "data": {
    "items": [...],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalItems": 150,
      "totalPages": 8,
      "hasNext": true,
      "hasPrevious": false
    }
  },
  "success": true
}
```

## 2. User Service API

### 2.1 ユーザー管理

#### 2.1.1 ユーザー情報取得

```http
GET /api/v1/users/{userId}
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "田中太郎",
    "email": "tanaka@example.com",
    "role": "Child",
    "birthDate": "2015-04-01",
    "parentId": "123e4567-e89b-12d3-a456-426614174001",
    "profile": {
      "avatarUrl": "https://storage.azure.com/avatars/user123.jpg",
      "theme": "blue",
      "language": "ja-JP"
    },
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 2.1.2 ユーザー登録

```http
POST /api/v1/users
Content-Type: application/json
Authorization: Bearer {token}

{
  "name": "田中花子",
  "email": "hanako@example.com",
  "role": "Child",
  "birthDate": "2016-08-15",
  "parentId": "123e4567-e89b-12d3-a456-426614174001"
}
```

**レスポンス例:**
```json
{
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174002",
    "name": "田中花子",
    "email": "hanako@example.com",
    "role": "Child",
    "birthDate": "2016-08-15",
    "parentId": "123e4567-e89b-12d3-a456-426614174001",
    "createdAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 2.1.3 子どもリスト取得（親用）

```http
GET /api/v1/users/parent/{parentId}/children
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "children": [
      {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "name": "田中太郎",
        "birthDate": "2015-04-01",
        "currentBalance": 2500,
        "activeGoals": 2
      },
      {
        "id": "123e4567-e89b-12d3-a456-426614174002",
        "name": "田中花子",
        "birthDate": "2016-08-15",
        "currentBalance": 1800,
        "activeGoals": 1
      }
    ]
  },
  "success": true
}
```

## 3. Account Service API

### 3.1 口座管理

#### 3.1.1 口座残高照会

```http
GET /api/v1/accounts/{userId}/balance
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "balance": 2500,
    "lastUpdated": "2024-12-14T10:30:00Z",
    "account": {
      "id": "acc_123456789",
      "accountName": "太郎のお小遣い口座",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  },
  "success": true
}
```

#### 3.1.2 残高履歴取得

```http
GET /api/v1/accounts/{accountId}/history?page=1&size=20&startDate=2024-12-01&endDate=2024-12-31
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "items": [
      {
        "id": "hist_123456789",
        "previousBalance": 2000,
        "newBalance": 2500,
        "changeAmount": 500,
        "changeType": "Increase",
        "reason": "お手伝いによる収入",
        "transactionId": "trans_123456789",
        "createdAt": "2024-12-14T10:30:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalItems": 45,
      "totalPages": 3,
      "hasNext": true,
      "hasPrevious": false
    }
  },
  "success": true
}
```

## 4. Transaction Service API

### 4.1 取引管理

#### 4.1.1 取引記録

```http
POST /api/v1/transactions
Content-Type: application/json
Authorization: Bearer {token}

{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "type": "Income",
  "amount": 500,
  "categoryId": "cat_123456789",
  "description": "お皿洗いのお手伝い",
  "transactionDate": "2024-12-14"
}
```

**レスポンス例:**
```json
{
  "data": {
    "id": "trans_123456789",
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "type": "Income",
    "amount": 500,
    "category": {
      "id": "cat_123456789",
      "name": "お手伝い",
      "iconName": "help",
      "color": "#4CAF50"
    },
    "description": "お皿洗いのお手伝い",
    "transactionDate": "2024-12-14",
    "createdAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 4.1.2 取引履歴取得

```http
GET /api/v1/transactions/user/{userId}?page=1&size=20&type=Income&startDate=2024-12-01&endDate=2024-12-31
Authorization: Bearer {token}
```

**クエリパラメータ:**
| パラメータ | 型 | 必須 | 説明 |
|------------|----|----|------|
| page | int | ○ | ページ番号（1から開始） |
| size | int | ○ | 1ページあたりの件数（最大100） |
| type | string | × | 取引タイプ（Income/Expense） |
| categoryId | string | × | カテゴリID |
| startDate | date | × | 開始日（YYYY-MM-DD） |
| endDate | date | × | 終了日（YYYY-MM-DD） |

**レスポンス例:**
```json
{
  "data": {
    "items": [
      {
        "id": "trans_123456789",
        "type": "Income",
        "amount": 500,
        "category": {
          "name": "お手伝い",
          "iconName": "help",
          "color": "#4CAF50"
        },
        "description": "お皿洗いのお手伝い",
        "transactionDate": "2024-12-14",
        "createdAt": "2024-12-14T10:30:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalItems": 78,
      "totalPages": 4,
      "hasNext": true,
      "hasPrevious": false
    }
  },
  "success": true
}
```

#### 4.1.3 カテゴリ別集計

```http
GET /api/v1/transactions/user/{userId}/summary?period=month&year=2024&month=12
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "period": "2024-12",
    "totalIncome": 3000,
    "totalExpense": 1500,
    "netAmount": 1500,
    "incomeByCategory": [
      {
        "category": "お手伝い",
        "amount": 2000,
        "transactionCount": 8,
        "percentage": 66.7
      },
      {
        "category": "お年玉",
        "amount": 1000,
        "transactionCount": 1,
        "percentage": 33.3
      }
    ],
    "expenseByCategory": [
      {
        "category": "お菓子",
        "amount": 800,
        "transactionCount": 12,
        "percentage": 53.3
      },
      {
        "category": "文房具",
        "amount": 700,
        "transactionCount": 3,
        "percentage": 46.7
      }
    ]
  },
  "success": true
}
```

#### 4.1.4 カテゴリマスタ取得

```http
GET /api/v1/transactions/categories?type=Income
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "categories": [
      {
        "id": "cat_123456789",
        "name": "お手伝い",
        "type": "Income",
        "iconName": "help",
        "color": "#4CAF50",
        "isDefault": true
      },
      {
        "id": "cat_123456790",
        "name": "お年玉",
        "type": "Income",
        "iconName": "gift",
        "color": "#2196F3",
        "isDefault": true
      }
    ]
  },
  "success": true
}
```

## 5. Goal Service API

### 5.1 目標管理

#### 5.1.1 目標作成

```http
POST /api/v1/goals
Content-Type: application/json
Authorization: Bearer {token}

{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "title": "新しいゲーム",
  "description": "欲しかったゲームソフトを買うため",
  "targetAmount": 5000,
  "targetDate": "2025-03-01",
  "priority": 2
}
```

**レスポンス例:**
```json
{
  "data": {
    "id": "goal_123456789",
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "title": "新しいゲーム",
    "description": "欲しかったゲームソフトを買うため",
    "targetAmount": 5000,
    "currentAmount": 0,
    "targetDate": "2025-03-01",
    "status": "Active",
    "priority": 2,
    "progressPercentage": 0,
    "daysRemaining": 77,
    "createdAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 5.1.2 目標一覧取得

```http
GET /api/v1/goals/user/{userId}?status=Active&page=1&size=10
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "items": [
      {
        "id": "goal_123456789",
        "title": "新しいゲーム",
        "targetAmount": 5000,
        "currentAmount": 1500,
        "targetDate": "2025-03-01",
        "status": "Active",
        "priority": 2,
        "progressPercentage": 30,
        "daysRemaining": 77
      },
      {
        "id": "goal_123456790",
        "title": "自転車",
        "targetAmount": 15000,
        "currentAmount": 8000,
        "targetDate": "2025-06-01",
        "status": "Active",
        "priority": 3,
        "progressPercentage": 53.3,
        "daysRemaining": 169
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 10,
      "totalItems": 2,
      "totalPages": 1,
      "hasNext": false,
      "hasPrevious": false
    }
  },
  "success": true
}
```

#### 5.1.3 目標進捗更新

```http
PUT /api/v1/goals/{goalId}/progress
Content-Type: application/json
Authorization: Bearer {token}

{
  "amount": 500,
  "note": "今月のお手伝い分を貯金"
}
```

**レスポンス例:**
```json
{
  "data": {
    "goalId": "goal_123456789",
    "previousAmount": 1500,
    "newAmount": 2000,
    "addedAmount": 500,
    "progressPercentage": 40,
    "note": "今月のお手伝い分を貯金",
    "updatedAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 5.1.4 目標達成

```http
PUT /api/v1/goals/{goalId}/complete
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "goalId": "goal_123456789",
    "title": "新しいゲーム",
    "targetAmount": 5000,
    "finalAmount": 5000,
    "status": "Completed",
    "completedAt": "2024-12-14T10:30:00Z",
    "achievementDays": 45
  },
  "success": true
}
```

## 6. Notification Service API

### 6.1 通知管理

#### 6.1.1 通知送信

```http
POST /api/v1/notifications
Content-Type: application/json
Authorization: Bearer {token}

{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "recipientId": "123e4567-e89b-12d3-a456-426614174001",
  "type": "TransactionAlert",
  "title": "取引通知",
  "message": "太郎くんがお菓子を100円で購入しました"
}
```

**レスポンス例:**
```json
{
  "data": {
    "id": "notif_123456789",
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "recipientId": "123e4567-e89b-12d3-a456-426614174001",
    "type": "TransactionAlert",
    "title": "取引通知",
    "message": "太郎くんがお菓子を100円で購入しました",
    "isRead": false,
    "createdAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 6.1.2 通知一覧取得

```http
GET /api/v1/notifications/user/{userId}?isRead=false&page=1&size=20
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "items": [
      {
        "id": "notif_123456789",
        "type": "TransactionAlert",
        "title": "取引通知",
        "message": "太郎くんがお菓子を100円で購入しました",
        "isRead": false,
        "createdAt": "2024-12-14T10:30:00Z"
      },
      {
        "id": "notif_123456790",
        "type": "GoalProgress",
        "title": "目標進捗",
        "message": "新しいゲームの貯金目標が50%に達しました",
        "isRead": false,
        "createdAt": "2024-12-13T15:20:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalItems": 8,
      "totalPages": 1,
      "hasNext": false,
      "hasPrevious": false
    }
  },
  "success": true
}
```

#### 6.1.3 通知設定更新

```http
PUT /api/v1/notifications/settings/{userId}
Content-Type: application/json
Authorization: Bearer {token}

{
  "emailNotifications": true,
  "smsNotifications": false,
  "pushNotifications": true,
  "transactionAlerts": true,
  "goalReminders": true,
  "monthlyReports": true
}
```

## 7. Report Service API

### 7.1 レポート生成

#### 7.1.1 月次レポート取得

```http
GET /api/v1/reports/monthly/{userId}?year=2024&month=12
Authorization: Bearer {token}
```

**レスポンス例:**
```json
{
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "period": {
      "year": 2024,
      "month": 12,
      "displayName": "2024年12月"
    },
    "summary": {
      "startingBalance": 2000,
      "totalIncome": 3000,
      "totalExpense": 1500,
      "netAmount": 1500,
      "endingBalance": 3500,
      "transactionCount": 20
    },
    "incomeAnalysis": {
      "byCategory": [
        {
          "category": "お手伝い",
          "amount": 2000,
          "percentage": 66.7,
          "transactionCount": 8
        },
        {
          "category": "お年玉",
          "amount": 1000,
          "percentage": 33.3,
          "transactionCount": 1
        }
      ],
      "dailyTrend": [
        {"date": "2024-12-01", "amount": 100},
        {"date": "2024-12-02", "amount": 200}
      ]
    },
    "expenseAnalysis": {
      "byCategory": [
        {
          "category": "お菓子",
          "amount": 800,
          "percentage": 53.3,
          "transactionCount": 12
        },
        {
          "category": "文房具",
          "amount": 700,
          "percentage": 46.7,
          "transactionCount": 3
        }
      ],
      "dailyTrend": [
        {"date": "2024-12-01", "amount": 50},
        {"date": "2024-12-02", "amount": 100}
      ]
    },
    "goalProgress": [
      {
        "goalTitle": "新しいゲーム",
        "targetAmount": 5000,
        "previousAmount": 1000,
        "currentAmount": 1500,
        "monthlyProgress": 500,
        "progressPercentage": 30
      }
    ],
    "insights": [
      "今月は先月より500円多く貯金できました！",
      "お菓子の支出が少し多めです。来月は気をつけてみましょう。",
      "目標の「新しいゲーム」まであと3500円です！"
    ],
    "generatedAt": "2024-12-14T10:30:00Z"
  },
  "success": true
}
```

#### 7.1.2 年次レポート取得

```http
GET /api/v1/reports/yearly/{userId}?year=2024
Authorization: Bearer {token}
```

#### 7.1.3 カテゴリ別分析

```http
GET /api/v1/reports/category-analysis/{userId}?period=6months&startDate=2024-07-01&endDate=2024-12-31
Authorization: Bearer {token}
```

## 8. エラーコード一覧

### 8.1 共通エラーコード

| コード | HTTPステータス | 説明 |
|--------|---------------|------|
| VALIDATION_ERROR | 400 | 入力データの検証エラー |
| UNAUTHORIZED | 401 | 認証が必要 |
| FORBIDDEN | 403 | アクセス権限なし |
| NOT_FOUND | 404 | リソースが見つからない |
| CONFLICT | 409 | データの競合状態 |
| RATE_LIMIT_EXCEEDED | 429 | レート制限に達している |
| INTERNAL_ERROR | 500 | サーバー内部エラー |
| SERVICE_UNAVAILABLE | 503 | サービス利用不可 |

### 8.2 業務固有エラーコード

| コード | HTTPステータス | 説明 |
|--------|---------------|------|
| INSUFFICIENT_BALANCE | 400 | 残高不足 |
| GOAL_ALREADY_COMPLETED | 400 | 目標は既に達成済み |
| INVALID_TRANSACTION_AMOUNT | 400 | 不正な取引金額 |
| PARENT_CHILD_RELATIONSHIP_REQUIRED | 400 | 親子関係が必要 |
| GOAL_TARGET_DATE_PAST | 400 | 目標日が過去の日付 |
| DUPLICATE_GOAL_TITLE | 409 | 同じタイトルの目標が既に存在 |
| USER_NOT_ACTIVE | 400 | ユーザーがアクティブでない |

## 9. WebSocket API（リアルタイム通知）

### 9.1 接続仕様

```javascript
// WebSocket接続
const socket = new WebSocket('wss://api.kidsmoneynoteweb.azurecontainerapps.io/ws');

// 認証
socket.send(JSON.stringify({
  type: 'auth',
  token: 'Bearer {jwt_token}'
}));

// 通知受信
socket.onmessage = function(event) {
  const notification = JSON.parse(event.data);
  handleNotification(notification);
};
```

### 9.2 通知メッセージ形式

```json
{
  "type": "notification",
  "data": {
    "id": "notif_123456789",
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "notificationType": "TransactionAlert",
    "title": "取引通知",
    "message": "太郎くんがお菓子を100円で購入しました",
    "timestamp": "2024-12-14T10:30:00Z"
  }
}
```

## 10. API テスト例

### 10.1 Postmanコレクション例

```json
{
  "info": {
    "name": "Kids Money Note API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "auth": {
    "type": "bearer",
    "bearer": [
      {
        "key": "token",
        "value": "{{jwt_token}}",
        "type": "string"
      }
    ]
  },
  "variable": [
    {
      "key": "base_url",
      "value": "https://api.kidsmoneynoteweb.azurecontainerapps.io/api/v1"
    },
    {
      "key": "user_id",
      "value": "123e4567-e89b-12d3-a456-426614174000"
    }
  ]
}
```

### 10.2 cURLサンプル

```bash
# ユーザー情報取得
curl -X GET \
  "https://api.kidsmoneynoteweb.azurecontainerapps.io/api/v1/users/123e4567-e89b-12d3-a456-426614174000" \
  -H "Authorization: Bearer ${JWT_TOKEN}" \
  -H "Content-Type: application/json"

# 取引記録
curl -X POST \
  "https://api.kidsmoneynoteweb.azurecontainerapps.io/api/v1/transactions" \
  -H "Authorization: Bearer ${JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "type": "Income",
    "amount": 500,
    "categoryId": "cat_123456789",
    "description": "お皿洗いのお手伝い",
    "transactionDate": "2024-12-14"
  }'
```

## 11. API バージョニング

### 11.1 バージョニング戦略
- **URLパス方式**: `/api/v1/`, `/api/v2/`
- **後方互換性**: 最低2つのメジャーバージョンをサポート
- **廃止予定API**: 6ヶ月前に廃止予告

### 11.2 変更管理
- **マイナーバージョン**: 新機能追加、非破壊的変更
- **メジャーバージョン**: 破壊的変更、アーキテクチャ変更
- **パッチバージョン**: バグ修正