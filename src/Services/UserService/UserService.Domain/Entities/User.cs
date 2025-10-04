namespace UserService.Domain.Entities;

/// <summary>
/// ユーザーエンティティ
/// 子どもと親の両方を管理します
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime BirthDate { get; set; }
    public Guid? ParentId { get; set; } // 子どもの場合の親ID
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ナビゲーションプロパティ
    public User? Parent { get; set; }
    public List<User> Children { get; set; } = new();
    public UserProfile? Profile { get; set; }
}

/// <summary>
/// ユーザーロール
/// </summary>
public enum UserRole
{
    Child = 0,
    Parent = 1
}

/// <summary>
/// ユーザープロファイルエンティティ
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = "default";
    public string Language { get; set; } = "ja-JP";
    public string TimeZone { get; set; } = "Asia/Tokyo";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ナビゲーションプロパティ
    public User User { get; set; } = null!;
}
