using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UserService.API.Validators;
using UserService.Domain.Repositories;
using UserService.Domain.Services;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enumを文字列として シリアライズ
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // プロパティ名をCamelCaseに設定
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Swagger/OpenAPI設定
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "User Service API", 
        Version = "v1",
        Description = "こども用お小遣い管理アプリケーション - ユーザー管理サービス"
    });
});

// データベース設定
builder.Services.AddDbContext<UserDbContext>(options =>
{
    // 開発環境ではInMemoryデータベースを使用
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("UserDb");
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("データベース接続文字列が設定されていません。");
        options.UseSqlServer(connectionString);
    }
});

// FluentValidation設定
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// 依存性注入の設定
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserDomainService, UserDomainService>();

// ヘルスチェック設定
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserDbContext>();

// CORS設定（開発環境）
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

var app = builder.Build();

// データベース初期化（開発環境）
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        context.Database.EnsureCreated();
        
        // テストデータの投入
        if (!context.Users.Any())
        {
            var parentUser = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Name = "田中花子",
                Email = "hanako.tanako@example.com",
                Role = UserService.Domain.Entities.UserRole.Parent,
                BirthDate = new DateTime(1985, 5, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var childUser = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Name = "田中太郎",
                Email = "taro.tanaka@example.com",
                Role = UserService.Domain.Entities.UserRole.Child,
                BirthDate = new DateTime(2015, 4, 1),
                ParentId = parentUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(parentUser, childUser);
            context.SaveChanges();
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
        c.RoutePrefix = string.Empty; // Swaggerをルートパスで表示
    });
    
    app.UseCors("Development");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// ヘルスチェックエンドポイント
app.MapHealthChecks("/health");

app.Run();
