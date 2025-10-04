namespace UserService.Domain.DTOs;

/// <summary>
/// ユーザー情報のデータ転送オブジェクト
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserProfileDto? Profile { get; set; }
}

/// <summary>
/// ユーザープロファイルのデータ転送オブジェクト
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}

/// <summary>
/// ユーザー作成リクエスト
/// </summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Child" or "Parent"
    public DateTime BirthDate { get; set; }
    public Guid? ParentId { get; set; }
}

/// <summary>
/// ユーザー更新リクエスト
/// </summary>
public class UpdateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
}

/// <summary>
/// ユーザープロファイル更新リクエスト
/// </summary>
public class UpdateUserProfileRequest
{
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}