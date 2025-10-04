using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data;

/// <summary>
/// ユーザーサービス用のデータベースコンテキスト
/// </summary>
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Userエンティティの設定
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.BirthDate)
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // 自己参照リレーション（親子関係）
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserProfileとの1対1リレーション
            entity.HasOne(e => e.Profile)
                .WithOne(e => e.User)
                .HasForeignKey<UserProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // インデックスの設定
            entity.HasIndex(e => e.ParentId);
        });

        // UserProfileエンティティの設定
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500);
            
            entity.Property(e => e.Theme)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("default");
            
            entity.Property(e => e.Language)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("ja-JP");
            
            entity.Property(e => e.TimeZone)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Asia/Tokyo");
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // ユニークインデックス
            entity.HasIndex(e => e.UserId)
                .IsUnique();
        });
    }
}
