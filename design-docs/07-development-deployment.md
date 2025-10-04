# 開発・デプロイメント設計書

## 1. 開発環境設定

### 1.1 前提条件

#### 1.1.1 必要なソフトウェア
- **.NET 9.0 SDK**: 最新版
- **Visual Studio 2022**: v17.8以降 または Visual Studio Code
- **Docker Desktop**: コンテナ開発用
- **Azure CLI**: v2.55以降
- **Git**: バージョン管理
- **Node.js**: v20.x (フロントエンド開発用)

#### 1.1.2 推奨開発ツール
- **Azure Data Studio**: データベース管理
- **Postman**: API テスト
- **Azure Storage Explorer**: ストレージ管理
- **Kubernetes CLI (kubectl)**: コンテナオーケストレーション

### 1.2 プロジェクト構造

```
KidsMoneyNote/
├── src/
│   ├── Services/
│   │   ├── UserService/
│   │   │   ├── UserService.API/
│   │   │   ├── UserService.Domain/
│   │   │   ├── UserService.Infrastructure/
│   │   │   └── UserService.Tests/
│   │   ├── AccountService/
│   │   ├── TransactionService/
│   │   ├── GoalService/
│   │   ├── NotificationService/
│   │   └── ReportService/
│   ├── ApiGateway/
│   │   └── ApiGateway/
│   ├── Web/
│   │   ├── KidsApp/
│   │   └── ParentApp/
│   └── Shared/
│       ├── CommonLibrary/
│       ├── EventBus/
│       └── Identity/
├── tests/
│   ├── IntegrationTests/
│   ├── LoadTests/
│   └── E2ETests/
├── infrastructure/
│   ├── bicep/
│   ├── terraform/ (optional)
│   └── kubernetes/
├── docs/
│   └── design-docs/
├── .github/
│   └── workflows/
├── docker-compose.yml
├── docker-compose.override.yml
└── KidsMoneyNote.sln
```

### 1.3 ローカル開発環境構築

#### 1.3.1 Docker Compose設定

```yaml
# docker-compose.yml
version: '3.8'

services:
  # データベース
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  # Redis (キャッシュ)
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data

  # RabbitMQ (メッセージング - 開発環境用)
  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin
    volumes:
      - rabbitmqdata:/var/lib/rabbitmq

  # User Service
  user-service:
    build:
      context: .
      dockerfile: src/Services/UserService/UserService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=UserDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - EventBus__Connection=amqp://admin:admin@rabbitmq:5672
    ports:
      - "8001:8080"
    depends_on:
      - sqlserver
      - rabbitmq

  # Account Service
  account-service:
    build:
      context: .
      dockerfile: src/Services/AccountService/AccountService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=AccountDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - EventBus__Connection=amqp://admin:admin@rabbitmq:5672
    ports:
      - "8002:8080"
    depends_on:
      - sqlserver
      - rabbitmq

  # Transaction Service
  transaction-service:
    build:
      context: .
      dockerfile: src/Services/TransactionService/TransactionService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=TransactionDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - EventBus__Connection=amqp://admin:admin@rabbitmq:5672
    ports:
      - "8003:8080"
    depends_on:
      - sqlserver
      - rabbitmq

  # API Gateway
  api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateway/ApiGateway/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Services__UserService=http://user-service:8080
      - Services__AccountService=http://account-service:8080
      - Services__TransactionService=http://transaction-service:8080
    ports:
      - "8000:8080"
    depends_on:
      - user-service
      - account-service
      - transaction-service

  # Kids Web App
  kids-web:
    build:
      context: .
      dockerfile: src/Web/KidsApp/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ApiGateway__BaseUrl=http://api-gateway:8080
    ports:
      - "8081:8080"
    depends_on:
      - api-gateway

volumes:
  sqldata:
  redisdata:
  rabbitmqdata:
```

#### 1.3.2 開発環境起動手順

```bash
# 1. リポジトリのクローン
git clone https://github.com/tsubakimoto/aspnetcore-microservices-kids-money-note.git
cd aspnetcore-microservices-kids-money-note

# 2. 環境変数設定
cp .env.example .env
# .envファイルを編集して必要な設定値を入力

# 3. Docker Composeでサービス起動
docker-compose up -d

# 4. データベースマイグレーション実行
dotnet run --project src/Services/UserService/UserService.API -- --migrate
dotnet run --project src/Services/AccountService/AccountService.API -- --migrate
dotnet run --project src/Services/TransactionService/TransactionService.API -- --migrate

# 5. 初期データの投入
dotnet run --project src/Services/UserService/UserService.API -- --seed
dotnet run --project src/Services/TransactionService/TransactionService.API -- --seed
```

### 1.4 Visual Studio 設定

#### 1.4.1 複数プロジェクト起動設定

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "compounds": [
    {
      "name": "Launch All Services",
      "configurations": [
        "Launch User Service",
        "Launch Account Service", 
        "Launch Transaction Service",
        "Launch API Gateway",
        "Launch Kids Web"
      ],
      "stopAll": true
    }
  ],
  "configurations": [
    {
      "name": "Launch User Service",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/Services/UserService/UserService.API/bin/Debug/net9.0/UserService.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Services/UserService/UserService.API",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7001;http://localhost:8001"
      }
    }
    // 他のサービスも同様に設定...
  ]
}
```

#### 1.4.2 デバッグ設定

```json
// appsettings.Development.json (各サービス共通)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database={ServiceName}Db;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "EventBus": {
    "Connection": "amqp://admin:admin@localhost:5672"
  },
  "Consul": {
    "Host": "localhost",
    "Port": 8500
  }
}
```

## 2. テスト戦略

### 2.1 テストレベル

#### 2.1.1 単体テスト
- **対象**: ドメインロジック、ビジネスルール
- **フレームワーク**: xUnit, FluentAssertions, Moq
- **カバレッジ目標**: 80%以上

```csharp
// 単体テスト例
public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockEventBus = new Mock<IEventBus>();
        _service = new TransactionService(_mockRepository.Object, _mockEventBus.Object);
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_ShouldCreateAndPublishEvent()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            UserId = Guid.NewGuid(),
            Amount = 100,
            Type = TransactionType.Income,
            Description = "Test transaction"
        };

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTransactionAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(100);
        _mockEventBus.Verify(x => x.PublishAsync(It.IsAny<TransactionCreatedEvent>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateTransaction_WithInvalidAmount_ShouldThrowValidationException(decimal invalidAmount)
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            UserId = Guid.NewGuid(),
            Amount = invalidAmount,
            Type = TransactionType.Income,
            Description = "Test transaction"
        };

        // Act & Assert
        await _service.Invoking(s => s.CreateTransactionAsync(request))
                     .Should().ThrowAsync<ValidationException>();
    }
}
```

#### 2.1.2 統合テスト
- **対象**: API エンドポイント、データベース連携
- **フレームワーク**: ASP.NET Core Test Host, Testcontainers

```csharp
// 統合テスト例
public class TransactionControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_ShouldReturn201()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            UserId = TestData.ValidUserId,
            Amount = 100,
            Type = TransactionType.Income,
            CategoryId = TestData.ValidCategoryId,
            Description = "Test transaction"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        responseContent.Data.Amount.Should().Be(100);
    }
}

// カスタムWebApplicationFactory
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // テスト用データベース設定
            services.RemoveAll<DbContextOptions<TransactionDbContext>>();
            services.AddDbContext<TransactionDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // テストデータシーダー
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            TestDataSeeder.SeedTestData(context);
        });
    }
}
```

#### 2.1.3 E2Eテスト
- **対象**: ユーザーシナリオ全体
- **フレームワーク**: Playwright, Selenium

```csharp
// E2Eテスト例
[TestClass]
public class KidsAllowanceE2ETests
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;

    [TestInitialize]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = false });
        _page = await _browser.NewPageAsync();
    }

    [TestMethod]
    public async Task CompleteAllowanceFlow_ShouldWorkEndToEnd()
    {
        // ログイン
        await _page.GotoAsync("https://localhost:8081");
        await _page.FillAsync("#email", "test.child@example.com");
        await _page.FillAsync("#password", "Test123!");
        await _page.ClickAsync("#login-button");

        // 取引記録
        await _page.ClickAsync("#add-transaction");
        await _page.SelectOptionAsync("#transaction-type", "Income");
        await _page.FillAsync("#amount", "500");
        await _page.SelectOptionAsync("#category", "お手伝い");
        await _page.FillAsync("#description", "お皿洗い");
        await _page.ClickAsync("#save-transaction");

        // 残高確認
        var balance = await _page.TextContentAsync("#current-balance");
        Assert.IsTrue(balance.Contains("500"));

        // 目標設定
        await _page.ClickAsync("#add-goal");
        await _page.FillAsync("#goal-title", "新しいゲーム");
        await _page.FillAsync("#target-amount", "5000");
        await _page.ClickAsync("#save-goal");

        // 目標進捗確認
        var progress = await _page.TextContentAsync("#goal-progress");
        Assert.IsTrue(progress.Contains("10%")); // 500/5000
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _browser?.CloseAsync();
        _playwright?.Dispose();
    }
}
```

### 2.2 負荷テスト

#### 2.2.1 NBomberを使用した負荷テスト

```csharp
// 負荷テスト例
public class LoadTests
{
    [Test]
    public void TransactionApiLoadTest()
    {
        var scenario = Scenario.Create("transaction_api_test", async context =>
        {
            var httpClient = new HttpClient();
            var request = new CreateTransactionRequest
            {
                UserId = TestData.GetRandomUserId(),
                Amount = Random.Shared.Next(1, 1000),
                Type = TransactionType.Income,
                CategoryId = TestData.GetRandomCategoryId(),
                Description = $"Load test transaction {context.ScenarioInfo.CurrentCopyNumber}"
            };

            var response = await httpClient.PostAsJsonAsync(
                "https://api.kidsmoneynoteweb.azurecontainerapps.io/api/v1/transactions", 
                request);

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(5)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(10))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
```

## 3. CI/CD パイプライン

### 3.1 GitHub Actions ワークフロー

#### 3.1.1 ビルド・テストワークフロー

```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '9.0.x'
  AZURE_CONTAINER_REGISTRY: kidsmoneynoteacr
  
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: YourStrong@Passw0rd
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q 'SELECT 1'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run unit tests
      run: |
        dotnet test tests/UnitTests/ \
          --no-build \
          --configuration Release \
          --logger trx \
          --collect:"XPlat Code Coverage" \
          --results-directory ./TestResults
    
    - name: Run integration tests
      run: |
        dotnet test tests/IntegrationTests/ \
          --no-build \
          --configuration Release \
          --logger trx \
          --results-directory ./TestResults
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost;Database=TestDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
    
    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: success() || failure()
      with:
        name: Test Results
        path: TestResults/*.trx
        reporter: dotnet-trx
    
    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
      with:
        directory: ./TestResults
        fail_ci_if_error: true

  security-scan:
    runs-on: ubuntu-latest
    needs: build-and-test
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'
    
    - name: Upload Trivy scan results
      uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: 'trivy-results.sarif'

  build-containers:
    runs-on: ubuntu-latest
    needs: [build-and-test, security-scan]
    if: github.ref == 'refs/heads/main'
    
    strategy:
      matrix:
        service: [user-service, account-service, transaction-service, goal-service, notification-service, report-service, api-gateway, kids-web, parent-web]
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Login to Azure Container Registry
      uses: azure/docker-login@v1
      with:
        login-server: ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io
        username: ${{ secrets.AZURE_ACR_USERNAME }}
        password: ${{ secrets.AZURE_ACR_PASSWORD }}
    
    - name: Build and push container image
      run: |
        IMAGE_TAG=${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/${{ matrix.service }}:${{ github.sha }}
        LATEST_TAG=${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/${{ matrix.service }}:latest
        
        docker build -f src/Services/${{ matrix.service }}/Dockerfile -t $IMAGE_TAG -t $LATEST_TAG .
        docker push $IMAGE_TAG
        docker push $LATEST_TAG

  deploy-dev:
    runs-on: ubuntu-latest
    needs: build-containers
    if: github.ref == 'refs/heads/develop'
    environment: development
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Deploy to development
      run: |
        az containerapp update \
          --name ca-user-service-dev \
          --resource-group rg-kids-money-note-dev \
          --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/user-service:${{ github.sha }}

  deploy-prod:
    runs-on: ubuntu-latest
    needs: build-containers
    if: github.ref == 'refs/heads/main'
    environment: production
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Deploy to production
      run: |
        # Blue-Green デプロイメント
        az containerapp revision copy \
          --name ca-user-service-prod \
          --resource-group rg-kids-money-note-prod \
          --from-revision latest \
          --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/user-service:${{ github.sha }}
```

#### 3.1.2 Infrastructure as Code デプロイ

```yaml
# .github/workflows/infrastructure.yml
name: Infrastructure Deployment

on:
  push:
    paths:
      - 'infrastructure/**'
    branches: [ main ]
  workflow_dispatch:

jobs:
  deploy-infrastructure:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Deploy Bicep template
      run: |
        az deployment group create \
          --resource-group rg-kids-money-note-prod \
          --template-file infrastructure/bicep/main.bicep \
          --parameters environment=prod \
                      location='Japan East' \
                      sqlAdminPassword='${{ secrets.SQL_ADMIN_PASSWORD }}'
```

### 3.2 デプロイメント戦略

#### 3.2.1 環境別デプロイメント

| 環境 | デプロイタイミング | 承認プロセス | ヘルスチェック |
|------|------------------|-------------|---------------|
| Development | 自動（develop ブランチ） | 不要 | 基本チェックのみ |
| Staging | 手動トリガー | 開発リーダー承認 | 全体テスト実行 |
| Production | 手動トリガー | マネージャー承認 | 段階的ロールアウト |

#### 3.2.2 Blue-Green デプロイメント

```bash
# Blue-Green デプロイメントスクリプト例
#!/bin/bash

# 現在のアクティブなリビジョンを取得
CURRENT_REVISION=$(az containerapp revision list \
  --name ca-user-service-prod \
  --resource-group rg-kids-money-note-prod \
  --query "[?properties.active].name" -o tsv)

# 新しいリビジョンを作成
NEW_REVISION=$(az containerapp revision copy \
  --name ca-user-service-prod \
  --resource-group rg-kids-money-note-prod \
  --from-revision $CURRENT_REVISION \
  --image $NEW_IMAGE \
  --query "name" -o tsv)

echo "新しいリビジョン作成完了: $NEW_REVISION"

# ヘルスチェック
HEALTH_CHECK_URL="https://ca-user-service-prod.azurecontainerapps.io/health"
for i in {1..30}; do
  if curl -f $HEALTH_CHECK_URL; then
    echo "ヘルスチェック成功"
    break
  fi
  echo "ヘルスチェック待機中... ($i/30)"
  sleep 10
done

# トラフィックを新しいリビジョンに切り替え
az containerapp ingress traffic set \
  --name ca-user-service-prod \
  --resource-group rg-kids-money-note-prod \
  --revision-weight $NEW_REVISION=100 $CURRENT_REVISION=0

echo "デプロイメント完了"

# 古いリビジョンを無効化（5分後）
sleep 300
az containerapp revision deactivate \
  --name ca-user-service-prod \
  --resource-group rg-kids-money-note-prod \
  --revision $CURRENT_REVISION
```

## 4. 監視・ログ設定

### 4.1 アプリケーション監視

#### 4.1.1 Application Insights設定

```csharp
// Program.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Application Insights
        builder.Services.AddApplicationInsightsTelemetry();
        
        // カスタムテレメトリ
        builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
        
        var app = builder.Build();
        
        // 監視ミドルウェア
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        app.UseMiddleware<PerformanceMonitoringMiddleware>();
        
        app.Run();
    }
}

// カスタムテレメトリ初期化
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "UserService";
        telemetry.Context.Component.Version = GetType().Assembly.GetName().Version?.ToString();
    }
}

// パフォーマンス監視ミドルウェア
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly TelemetryClient _telemetryClient;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var telemetry = new RequestTelemetry
            {
                Name = $"{context.Request.Method} {context.Request.Path}",
                Duration = stopwatch.Elapsed,
                ResponseCode = context.Response.StatusCode.ToString(),
                Success = context.Response.StatusCode < 400
            };
            
            _telemetryClient.TrackRequest(telemetry);
            
            // 遅いリクエストをログ出力
            if (stopwatch.ElapsedMilliseconds > 2000)
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMs}ms",
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

#### 4.1.2 構造化ログ設定

```json
// appsettings.json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.ApplicationInsights"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights",
          "connectionString": ""
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### 4.2 アラート設定

#### 4.2.1 Azure Monitor アラートルール

```bicep
// アラートルール設定
resource alertRules 'Microsoft.Insights/metricAlerts@2018-03-01' = [for rule in [
  {
    name: 'high-response-time'
    description: 'Response time is high'
    severity: 2
    windowSize: 'PT15M'
    evaluationFrequency: 'PT5M'
    metricName: 'requests/duration'
    operator: 'GreaterThan'
    threshold: 2000
  }
  {
    name: 'high-error-rate'
    description: 'Error rate is high'
    severity: 1
    windowSize: 'PT5M'
    evaluationFrequency: 'PT1M'
    metricName: 'requests/failed'
    operator: 'GreaterThan'
    threshold: 10
  }
  {
    name: 'low-availability'
    description: 'Availability is low'
    severity: 0
    windowSize: 'PT5M'
    evaluationFrequency: 'PT1M'
    metricName: 'availabilityResults/availabilityPercentage'
    operator: 'LessThan'
    threshold: 99
  }
]: {
  name: 'alert-${rule.name}-${environment}'
  location: 'global'
  properties: {
    description: rule.description
    severity: rule.severity
    enabled: true
    scopes: [applicationInsights.id]
    evaluationFrequency: rule.evaluationFrequency
    windowSize: rule.windowSize
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
```

## 5. 運用手順

### 5.1 日常運用

#### 5.1.1 健全性チェック

```bash
#!/bin/bash
# daily-health-check.sh

echo "=== Kids Money Note 日次健全性チェック ==="

# 各サービスのヘルスチェック
SERVICES=("user-service" "account-service" "transaction-service" "goal-service" "notification-service" "report-service")

for service in "${SERVICES[@]}"; do
  echo "Checking $service..."
  HEALTH_URL="https://ca-$service-prod.azurecontainerapps.io/health"
  
  if curl -f -s --max-time 30 $HEALTH_URL > /dev/null; then
    echo "✅ $service is healthy"
  else
    echo "❌ $service is unhealthy"
    # Slackに通知
    curl -X POST -H 'Content-type: application/json' \
      --data "{\"text\":\"⚠️ $service がヘルスチェックに失敗しました\"}" \
      $SLACK_WEBHOOK_URL
  fi
done

# データベース接続チェック
echo "Checking database connections..."
az sql db list --server sql-kids-money-note-prod --resource-group rg-kids-money-note-prod

# ログエラー率チェック
echo "Checking error rates..."
ERROR_RATE=$(az monitor metrics list \
  --resource /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-kids-money-note-prod/providers/Microsoft.Insights/components/appi-kids-money-note-prod \
  --metric 'requests/failed' \
  --interval 1h \
  --aggregation Average \
  --query 'value[0].timeseries[0].data[-1].average' -o tsv)

if (( $(echo "$ERROR_RATE > 5" | bc -l) )); then
  echo "❌ エラー率が高すぎます: $ERROR_RATE%"
else
  echo "✅ エラー率は正常範囲内: $ERROR_RATE%"
fi

echo "=== チェック完了 ==="
```

#### 5.1.2 バックアップ確認

```bash
#!/bin/bash
# backup-verification.sh

echo "=== バックアップ確認 ==="

# SQL Database バックアップ確認
DATABASES=("UserDb" "AccountDb" "TransactionDb" "GoalDb" "NotificationDb" "ReportDb")

for db in "${DATABASES[@]}"; do
  echo "Checking backup for $db-prod..."
  
  LATEST_BACKUP=$(az sql db list-deleted \
    --server sql-kids-money-note-prod \
    --resource-group rg-kids-money-note-prod \
    --query "[?contains(name, '$db')].deletionDate | max(@)" -o tsv)
  
  if [ -n "$LATEST_BACKUP" ]; then
    echo "✅ $db の最新バックアップ: $LATEST_BACKUP"
  else
    echo "⚠️ $db のバックアップが見つかりません"
  fi
done

# Blob Storage バックアップ確認
echo "Checking blob storage backups..."
az storage blob list \
  --container-name backups \
  --account-name stkidsmoneynote \
  --query "[?contains(name, '$(date +%Y-%m-%d)')].name" -o table

echo "=== バックアップ確認完了 ==="
```

### 5.2 インシデント対応

#### 5.2.1 障害対応手順

```bash
#!/bin/bash
# incident-response.sh

echo "=== インシデント対応 ==="

INCIDENT_LEVEL=$1  # P0, P1, P2, P3

case $INCIDENT_LEVEL in
  "P0")
    echo "🚨 P0 Critical Incident - 即座に対応開始"
    # 全サービス停止・緊急メンテナンス画面表示
    az containerapp update --name ca-api-gateway-prod --set-env-vars MAINTENANCE_MODE=true
    ;;
  "P1")
    echo "⚠️ P1 High Incident - 1時間以内に対応"
    # 影響のあるサービスのみ停止
    ;;
  "P2")
    echo "ℹ️ P2 Medium Incident - 4時間以内に対応"
    # 監視強化・ログ収集
    ;;
  "P3")
    echo "📝 P3 Low Incident - 24時間以内に対応"
    # 計画的メンテナンスで対応
    ;;
esac

# インシデント情報をSlackに通知
curl -X POST -H 'Content-type: application/json' \
  --data "{\"text\":\"🚨 $INCIDENT_LEVEL インシデントが発生しました。対応を開始してください。\"}" \
  $SLACK_WEBHOOK_URL

echo "=== インシデント対応完了 ==="
```

### 5.3 スケーリング

#### 5.3.1 手動スケーリング

```bash
#!/bin/bash
# manual-scaling.sh

SERVICE_NAME=$1
REPLICA_COUNT=$2

echo "Scaling $SERVICE_NAME to $REPLICA_COUNT replicas..."

az containerapp update \
  --name ca-$SERVICE_NAME-prod \
  --resource-group rg-kids-money-note-prod \
  --min-replicas $REPLICA_COUNT \
  --max-replicas $((REPLICA_COUNT * 2))

echo "Scaling completed"
```

#### 5.3.2 自動スケーリング設定

```bicep
// 自動スケーリング設定
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  properties: {
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
          {
            name: 'cpu-scaling'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70'
              }
            }
          }
          {
            name: 'memory-scaling'
            custom: {
              type: 'memory'
              metadata: {
                type: 'Utilization'
                value: '80'
              }
            }
          }
        ]
      }
    }
  }
}
```

## 6. パフォーマンス最適化

### 6.1 データベース最適化

#### 6.1.1 インデックス最適化

```sql
-- パフォーマンス分析クエリ
SELECT 
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    dm_ius.user_seeks,
    dm_ius.user_scans,
    dm_ius.user_lookups,
    dm_ius.user_updates
FROM sys.dm_db_index_usage_stats dm_ius
INNER JOIN sys.indexes i ON dm_ius.object_id = i.object_id AND dm_ius.index_id = i.index_id
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE dm_ius.database_id = DB_ID()
ORDER BY dm_ius.user_seeks + dm_ius.user_scans + dm_ius.user_lookups DESC;

-- 重いクエリの特定
SELECT TOP 10
    qs.sql_handle,
    qs.execution_count,
    qs.total_logical_reads,
    qs.total_logical_writes,
    qs.total_worker_time,
    qs.total_elapsed_time,
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(st.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS statement_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
ORDER BY qs.total_elapsed_time DESC;
```

### 6.2 キャッシュ戦略

#### 6.2.1 Redis キャッシュ実装

```csharp
// キャッシュサービス
public class CacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<CacheService> _logger;
    
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key {Key}", key);
            return default;
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key {Key}", key);
        }
    }
}

// キャッシュ戦略の適用
public class TransactionService
{
    private readonly ICacheService _cache;
    
    public async Task<TransactionSummary> GetMonthlySummaryAsync(Guid userId, int year, int month)
    {
        var cacheKey = $"monthly_summary:{userId}:{year}-{month:D2}";
        
        // キャッシュから取得を試行
        var cachedSummary = await _cache.GetAsync<TransactionSummary>(cacheKey);
        if (cachedSummary != null)
        {
            return cachedSummary;
        }
        
        // データベースから取得
        var summary = await _repository.GetMonthlySummaryAsync(userId, year, month);
        
        // キャッシュに保存（1時間）
        await _cache.SetAsync(cacheKey, summary, TimeSpan.FromHours(1));
        
        return summary;
    }
}
```

## 7. セキュリティ運用

### 7.1 セキュリティ監視

#### 7.1.1 セキュリティダッシュボード

```kusto
// Application Insights クエリ例
// 異常なログイン試行の検出
requests
| where timestamp > ago(1h)
| where url contains "/api/auth/login"
| where resultCode >= 400
| summarize FailedAttempts = count() by client_IP, bin(timestamp, 5m)
| where FailedAttempts > 5
| order by timestamp desc

// SQL インジェクション攻撃の検出
requests
| where timestamp > ago(24h)
| where url contains "'"
    or url contains "--"
    or url contains "union"
    or url contains "drop"
    or url contains "delete"
| project timestamp, client_IP, url, userAgent = tostring(customDimensions.UserAgent)
| order by timestamp desc
```

### 7.2 脆弱性管理

#### 7.2.1 依存関係の脆弱性チェック

```bash
#!/bin/bash
# vulnerability-scan.sh

echo "=== 脆弱性スキャン開始 ==="

# .NET 依存関係の脆弱性チェック
dotnet list package --vulnerable --include-transitive

# Docker イメージの脆弱性チェック
trivy image kidsmoneynoteacr.azurecr.io/user-service:latest

# npm 依存関係の脆弱性チェック（フロントエンド）
cd src/Web/KidsApp
npm audit

echo "=== 脆弱性スキャン完了 ==="
```