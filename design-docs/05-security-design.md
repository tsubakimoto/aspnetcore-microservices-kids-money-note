# セキュリティ設計書

## 1. セキュリティ概要

### 1.1 セキュリティ設計原則
- **最小権限の原則**: 必要最小限の権限のみ付与
- **多層防御**: 複数のセキュリティレイヤーで保護
- **Zero Trust**: 全ての通信を検証
- **暗号化**: データの転送時・保存時の暗号化
- **監査**: 全ての操作を記録・監視

### 1.2 脅威モデル

#### 1.2.1 識別された脅威
| 脅威 | リスクレベル | 対策 |
|------|-------------|------|
| 不正アクセス | 高 | 多要素認証、ロールベースアクセス制御 |
| データ漏洩 | 高 | 暗号化、アクセス制御、監査ログ |
| SQLインジェクション | 中 | パラメータ化クエリ、入力検証 |
| XSS攻撃 | 中 | 入力サニタイゼーション、CSP |
| CSRF攻撃 | 中 | アンチCSRFトークン |
| セッションハイジャック | 中 | HTTPS、セキュアCookie |
| DoS攻撃 | 中 | レート制限、WAF |

## 2. 認証・認可設計

### 2.1 Microsoft Entra ID統合

#### 2.1.1 アプリケーション登録

```json
{
  "displayName": "Kids Money Note",
  "identifierUris": ["api://kids-money-note"],
  "requiredResourceAccess": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "resourceAccess": [
        {
          "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
          "type": "Scope"
        }
      ]
    }
  ],
  "api": {
    "oauth2PermissionScopes": [
      {
        "id": "user.read",
        "adminConsentDescription": "Allow the application to read user data",
        "adminConsentDisplayName": "Read user data",
        "isEnabled": true,
        "type": "User",
        "userConsentDescription": "Allow the application to read your data",
        "userConsentDisplayName": "Read your data",
        "value": "user.read"
      }
    ]
  }
}
```

#### 2.1.2 ユーザー・ロール管理

```csharp
public enum UserRole
{
    Child,      // 子どもユーザー
    Parent,     // 親ユーザー
    Admin       // システム管理者
}

public class ClaimTypes
{
    public const string Role = "role";
    public const string UserId = "sub";
    public const string ParentId = "parent_id";
    public const string ChildIds = "child_ids";
}

// カスタムクレイム設定
public class CustomClaimsTransformation : IClaimsTransformation
{
    private readonly IUserService _userService;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userService.GetUserAsync(userId);
            if (user != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
                
                if (user.Role == UserRole.Child && user.ParentId.HasValue)
                {
                    identity.AddClaim(new Claim("parent_id", user.ParentId.Value.ToString()));
                }
                else if (user.Role == UserRole.Parent)
                {
                    var children = await _userService.GetChildrenAsync(user.Id);
                    foreach (var child in children)
                    {
                        identity.AddClaim(new Claim("child_ids", child.Id.ToString()));
                    }
                }
            }
        }
        
        return principal;
    }
}
```

### 2.2 JWT Token設定

#### 2.2.1 JWT設定

```csharp
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

// JWT認証設定
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}";
        options.Audience = jwtSettings.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

#### 2.2.2 カスタム認可ポリシー

```csharp
// 認可ポリシー設定
services.AddAuthorization(options =>
{
    // 子どもユーザーポリシー
    options.AddPolicy("ChildUser", policy =>
        policy.RequireClaim(ClaimTypes.Role, UserRole.Child.ToString()));
    
    // 親ユーザーポリシー
    options.AddPolicy("ParentUser", policy =>
        policy.RequireClaim(ClaimTypes.Role, UserRole.Parent.ToString()));
    
    // データアクセスポリシー
    options.AddPolicy("CanAccessUserData", policy =>
        policy.Requirements.Add(new UserDataAccessRequirement()));
    
    // 管理者ポリシー
    options.AddPolicy("AdminUser", policy =>
        policy.RequireClaim(ClaimTypes.Role, UserRole.Admin.ToString()));
});

// カスタム認可要件
public class UserDataAccessRequirement : IAuthorizationRequirement { }

public class UserDataAccessHandler : AuthorizationHandler<UserDataAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserDataAccessRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = context.User.FindFirst(ClaimTypes.Role);
        
        if (userIdClaim == null || roleClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }
        
        // HTTPコンテキストから要求されたユーザーIDを取得
        if (context.Resource is HttpContext httpContext)
        {
            var requestedUserId = httpContext.Request.RouteValues["userId"]?.ToString();
            
            if (string.IsNullOrEmpty(requestedUserId))
            {
                context.Fail();
                return Task.CompletedTask;
            }
            
            // 自分のデータまたは親が子どものデータにアクセスする場合
            if (userIdClaim.Value == requestedUserId ||
                (roleClaim.Value == UserRole.Parent.ToString() &&
                 context.User.HasClaim("child_ids", requestedUserId)))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        
        return Task.CompletedTask;
    }
}
```

## 3. データ保護

### 3.1 暗号化設計

#### 3.1.1 転送時暗号化
- **TLS 1.3**: 全ての通信にTLS 1.3を強制
- **HSTS**: HTTP Strict Transport Security適用
- **証明書**: Azure Key Vault管理のSSL証明書

```csharp
// HTTPS設定
public void ConfigureServices(IServiceCollection services)
{
    services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
    
    services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
        options.HttpsPort = 443;
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (!env.IsDevelopment())
    {
        app.UseHsts();
    }
    
    app.UseHttpsRedirection();
}
```

#### 3.1.2 保存時暗号化
- **Azure SQL Database**: Transparent Data Encryption (TDE)
- **Azure Storage**: 256-bit AES暗号化
- **Key Vault**: 秘密情報の暗号化保存

```csharp
// 機密データの暗号化ヘルパー
public class DataProtectionService
{
    private readonly IDataProtector _protector;
    
    public DataProtectionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("KidsMoneyNote.SensitiveData");
    }
    
    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }
    
    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}

// データ保護設定
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(connectionString, containerName, blobName)
    .ProtectKeysWithAzureKeyVault(keyVaultUrl, credential);
```

### 3.2 個人情報保護

#### 3.2.1 PIIデータの識別と保護

```csharp
// 個人情報属性
[AttributeUsage(AttributeTargets.Property)]
public class PersonalDataAttribute : Attribute
{
    public bool RequireEncryption { get; set; }
    public bool RequireMasking { get; set; }
}

// PIIデータモデル例
public class UserProfile
{
    public Guid Id { get; set; }
    
    [PersonalData(RequireMasking = true)]
    public string Name { get; set; } = string.Empty;
    
    [PersonalData(RequireEncryption = true, RequireMasking = true)]
    public string Email { get; set; } = string.Empty;
    
    [PersonalData(RequireMasking = true)]
    public DateTime BirthDate { get; set; }
}

// PIIマスキングサービス
public class PiiMaskingService
{
    public T MaskPersonalData<T>(T entity) where T : class
    {
        var type = typeof(T);
        var properties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<PersonalDataAttribute>()?.RequireMasking == true);
        
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(string))
            {
                var value = property.GetValue(entity) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    property.SetValue(entity, MaskString(value));
                }
            }
        }
        
        return entity;
    }
    
    private string MaskString(string input)
    {
        if (input.Length <= 2) return "***";
        return input[0] + new string('*', input.Length - 2) + input[^1];
    }
}
```

## 4. API セキュリティ

### 4.1 入力検証

#### 4.1.1 モデル検証

```csharp
// 入力検証属性
public class SafeStringAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is string str && ContainsMaliciousContent(str))
        {
            return new ValidationResult("入力に不正な文字が含まれています。");
        }
        return ValidationResult.Success;
    }
    
    private bool ContainsMaliciousContent(string input)
    {
        var maliciousPatterns = new[]
        {
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", // Script tags
            @"javascript:", // JavaScript protocol
            @"on\w+\s*=", // Event handlers
            @"expression\s*\(", // CSS expressions
        };
        
        return maliciousPatterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }
}

// APIモデル例
public class CreateTransactionRequest
{
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }
    
    [Required]
    [SafeString]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public Guid CategoryId { get; set; }
}
```

#### 4.1.2 SQLインジェクション対策

```csharp
// Entity Framework Core使用によるパラメータ化クエリ
public class TransactionRepository
{
    private readonly TransactionDbContext _context;
    
    public async Task<IEnumerable<Transaction>> GetTransactionsByUserAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && 
                       t.TransactionDate >= startDate && 
                       t.TransactionDate <= endDate)
            .ToListAsync();
    }
    
    // 動的クエリが必要な場合はパラメータ化を徹底
    public async Task<IEnumerable<Transaction>> SearchTransactionsAsync(
        Guid userId, string searchTerm)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .Where(t => EF.Functions.Like(t.Description, $"%{searchTerm}%"))
            .ToListAsync();
    }
}
```

### 4.2 レート制限

#### 4.2.1 レート制限設定

```csharp
// レート制限設定
public class RateLimitingSettings
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
}

// レート制限ミドルウェア
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitingSettings _settings;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var key = $"rate_limit_{clientId}";
        
        if (!_cache.TryGetValue(key, out RateLimitInfo rateLimitInfo))
        {
            rateLimitInfo = new RateLimitInfo();
            _cache.Set(key, rateLimitInfo, TimeSpan.FromMinutes(1));
        }
        
        if (rateLimitInfo.RequestCount >= _settings.RequestsPerMinute)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        rateLimitInfo.RequestCount++;
        await _next(context);
    }
    
    private string GetClientId(HttpContext context)
    {
        return context.User.Identity?.Name ?? 
               context.Connection.RemoteIpAddress?.ToString() ?? 
               "anonymous";
    }
}

public class RateLimitInfo
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; } = DateTime.UtcNow;
}
```

### 4.3 CORS設定

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("KidsMoneyNotePolicy", builder =>
        {
            builder.WithOrigins(
                    "https://kidsmoneynoteweb.azurecontainerapps.io",
                    "https://localhost:5001")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
    });
}

public void Configure(IApplicationBuilder app)
{
    app.UseCors("KidsMoneyNotePolicy");
}
```

## 5. セキュリティヘッダー

### 5.1 セキュリティヘッダー設定

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://apis.google.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' https://api.kidsmoneynoteweb.azurecontainerapps.io");
        
        // X-Frame-Options
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // X-Content-Type-Options
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // Referrer Policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Permissions Policy
        context.Response.Headers.Add("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=()");
        
        await _next(context);
    }
}
```

## 6. 監査・ログ

### 6.1 セキュリティ監査ログ

```csharp
// 監査ログエンティティ
public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; } = string.Empty;
}

// 監査ログサービス
public class AuditService
{
    private readonly AuditDbContext _context;
    private readonly ILogger<AuditService> _logger;
    
    public async Task LogAsync(AuditLog auditLog)
    {
        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            
            // 重要なセキュリティイベントは即座にAlert
            if (IsSecurityCritical(auditLog))
            {
                await SendSecurityAlert(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log");
        }
    }
    
    private bool IsSecurityCritical(AuditLog auditLog)
    {
        var criticalActions = new[]
        {
            "LOGIN_FAILED",
            "UNAUTHORIZED_ACCESS",
            "PERMISSION_ESCALATION",
            "DATA_EXPORT",
            "ADMIN_ACTION"
        };
        
        return criticalActions.Contains(auditLog.Action) || !auditLog.Success;
    }
}

// 監査ログ記録属性
public class AuditAttribute : ActionFilterAttribute
{
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context, 
        ActionExecutionDelegate next)
    {
        var auditService = context.HttpContext.RequestServices
            .GetRequiredService<AuditService>();
        
        var executedContext = await next();
        
        var auditLog = new AuditLog
        {
            UserId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            Action = Action,
            Resource = Resource,
            IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
            Success = executedContext.Exception == null,
            FailureReason = executedContext.Exception?.Message ?? "",
            Timestamp = DateTime.UtcNow,
            RequestId = context.HttpContext.TraceIdentifier
        };
        
        await auditService.LogAsync(auditLog);
    }
}
```

### 6.2 セキュリティモニタリング

```csharp
// セキュリティイベント検出
public class SecurityMonitoringService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly INotificationService _notificationService;
    
    public async Task MonitorLoginAttempts(string userId, bool success)
    {
        var key = $"login_attempts_{userId}";
        var attempts = await GetRecentLoginAttempts(userId);
        
        if (!success)
        {
            attempts.Add(DateTime.UtcNow);
            
            // 5分間に5回失敗でアカウントロック
            var recentFailures = attempts.Where(a => a > DateTime.UtcNow.AddMinutes(-5));
            if (recentFailures.Count() >= 5)
            {
                await LockUserAccount(userId);
                await SendSecurityAlert($"User {userId} account locked due to multiple failed login attempts");
            }
        }
        else
        {
            // 成功時は失敗カウントをリセット
            await ClearLoginAttempts(userId);
        }
    }
    
    public async Task MonitorUnusualActivity(string userId, string action)
    {
        var userActivities = await GetRecentUserActivities(userId);
        
        // 異常な活動パターンの検出
        if (IsUnusualActivityPattern(userActivities, action))
        {
            await SendSecurityAlert($"Unusual activity detected for user {userId}: {action}");
        }
    }
}
```

## 7. インシデント対応

### 7.1 セキュリティインシデント対応手順

#### 7.1.1 インシデントレベル分類

| レベル | 説明 | 対応時間 | 対応チーム |
|--------|------|----------|-----------|
| P0 - Critical | データ漏洩、システム侵害 | 15分以内 | セキュリティチーム全員 |
| P1 - High | 認証システム障害 | 1時間以内 | セキュリティチーム + 開発チーム |
| P2 - Medium | 不正アクセス試行 | 4時間以内 | セキュリティチーム |
| P3 - Low | セキュリティ設定不備 | 24時間以内 | 開発チーム |

#### 7.1.2 自動対応機能

```csharp
public class SecurityIncidentResponseService
{
    public async Task HandleSuspiciousActivity(SecurityIncident incident)
    {
        switch (incident.Severity)
        {
            case IncidentSeverity.Critical:
                await LockAllUserSessions();
                await NotifySecurityTeam(incident);
                await CreateEmergencyTicket(incident);
                break;
                
            case IncidentSeverity.High:
                await LockAffectedUserSessions(incident.AffectedUsers);
                await NotifySecurityTeam(incident);
                break;
                
            case IncidentSeverity.Medium:
                await EnhanceMonitoring(incident.AffectedResources);
                await NotifyOnCallEngineer(incident);
                break;
                
            case IncidentSeverity.Low:
                await LogIncident(incident);
                break;
        }
    }
}
```

## 8. コンプライアンス

### 8.1 データ保護規制対応

#### 8.1.1 GDPR対応
- **データ主体の権利**: アクセス、修正、削除、ポータビリティ
- **プライバシーバイデザイン**: 設計段階からの個人情報保護
- **データ処理の記録**: 全ての個人データ処理活動の記録

```csharp
// GDPR対応データ管理サービス
public class GdprDataService
{
    public async Task<PersonalDataExport> ExportUserDataAsync(Guid userId)
    {
        // 全サービスからユーザーの個人データを収集
        var userData = await _userService.GetUserDataAsync(userId);
        var transactions = await _transactionService.GetUserTransactionsAsync(userId);
        var goals = await _goalService.GetUserGoalsAsync(userId);
        
        return new PersonalDataExport
        {
            UserData = userData,
            Transactions = transactions,
            Goals = goals,
            ExportDate = DateTime.UtcNow
        };
    }
    
    public async Task DeleteUserDataAsync(Guid userId)
    {
        // 法的保持期間チェック
        var canDelete = await CanDeleteUserDataAsync(userId);
        if (!canDelete)
        {
            throw new InvalidOperationException("データは法的保持期間中のため削除できません");
        }
        
        // 全サービスからユーザーデータを削除
        await _userService.DeleteUserAsync(userId);
        await _transactionService.DeleteUserTransactionsAsync(userId);
        await _goalService.DeleteUserGoalsAsync(userId);
        
        // 監査ログに記録
        await _auditService.LogAsync(new AuditLog
        {
            Action = "DATA_DELETION",
            UserId = userId.ToString(),
            Success = true,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

## 9. セキュリティテスト

### 9.1 自動セキュリティテスト

```csharp
// セキュリティテスト例
[TestClass]
public class SecurityTests
{
    [TestMethod]
    public async Task Should_Reject_Unauthorized_Access()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/api/users/profile");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [TestMethod]
    public async Task Should_Prevent_SQL_Injection()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var maliciousInput = "'; DROP TABLE Users; --";
        
        // Act
        var response = await client.GetAsync($"/api/transactions/search?term={maliciousInput}");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [TestMethod]
    public async Task Should_Enforce_Rate_Limiting()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        
        // Act
        var tasks = Enumerable.Range(1, 100)
            .Select(_ => client.GetAsync("/api/users/profile"));
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.IsTrue(responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests));
    }
}
```

### 9.2 ペネトレーションテスト計画

| テスト項目 | 頻度 | 責任者 |
|------------|------|--------|
| Webアプリケーション脆弱性テスト | 四半期 | 外部セキュリティ会社 |
| APIエンドポイントテスト | 月次 | 内部セキュリティチーム |
| インフラ脆弱性テスト | 半年 | 外部セキュリティ会社 |
| ソーシャルエンジニアリングテスト | 年次 | 外部セキュリティ会社 |

## 10. セキュリティ運用

### 10.1 セキュリティダッシュボード

```csharp
// セキュリティメトリクス収集
public class SecurityMetricsService
{
    public async Task<SecurityDashboard> GetSecurityMetricsAsync()
    {
        return new SecurityDashboard
        {
            FailedLoginAttempts = await GetFailedLoginCount(TimeSpan.FromDays(1)),
            BlockedIPs = await GetBlockedIPCount(TimeSpan.FromDays(1)),
            SecurityAlerts = await GetSecurityAlertCount(TimeSpan.FromDays(7)),
            VulnerabilityCount = await GetOpenVulnerabilityCount(),
            ComplianceScore = await CalculateComplianceScore()
        };
    }
}

public class SecurityDashboard
{
    public int FailedLoginAttempts { get; set; }
    public int BlockedIPs { get; set; }
    public int SecurityAlerts { get; set; }
    public int VulnerabilityCount { get; set; }
    public decimal ComplianceScore { get; set; }
}
```

### 10.2 セキュリティ教育・訓練

#### 10.2.1 開発者向けセキュリティ教育
- **OWASP Top 10**: Webアプリケーションセキュリティリスク
- **セキュアコーディング**: 安全なコード記述方法
- **脆弱性対応**: インシデント対応手順

#### 10.2.2 定期的なセキュリティ訓練
- **フィッシング訓練**: 月次
- **インシデント対応訓練**: 四半期
- **セキュリティ意識向上研修**: 半年