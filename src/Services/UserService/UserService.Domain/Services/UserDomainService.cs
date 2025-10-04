using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Domain.Services;

/// <summary>
/// ユーザードメインサービス
/// ビジネスロジックを管理します
/// </summary>
public interface IUserDomainService
{
    /// <summary>
    /// ユーザー情報を取得
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ユーザーDTO</returns>
    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを作成
    /// </summary>
    /// <param name="request">ユーザー作成リクエスト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>作成されたユーザーDTO</returns>
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザー情報を更新
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="request">ユーザー更新リクエスト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>更新されたユーザーDTO</returns>
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 親IDで子どもリストを取得
    /// </summary>
    /// <param name="parentId">親ID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>子どもDTOリスト</returns>
    Task<List<UserDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを削除
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// ユーザードメインサービス実装
/// </summary>
public class UserDomainService : IUserDomainService
{
    private readonly IUserRepository _userRepository;

    public UserDomainService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.ToDto();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // バリデーション: メールアドレスの重複チェック
        if (await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"メールアドレス '{request.Email}' は既に使用されています。");
        }

        // 子どもの場合、親が存在するかチェック
        if (request.Role == "Child" && request.ParentId.HasValue)
        {
            var parent = await _userRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null || parent.Role != UserRole.Parent)
            {
                throw new InvalidOperationException("有効な親ユーザーが見つかりません。");
            }
        }

        // 親の場合、ParentIdは設定しない
        if (request.Role == "Parent" && request.ParentId.HasValue)
        {
            throw new InvalidOperationException("親ユーザーにはParentIdを設定できません。");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Role = Enum.Parse<UserRole>(request.Role),
            BirthDate = request.BirthDate,
            ParentId = request.ParentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);
        return createdUser.ToDto();
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"ユーザーID '{userId}' が見つかりません。");

        // バリデーション: メールアドレスの重複チェック（自分以外）
        if (await _userRepository.IsEmailExistsAsync(request.Email, userId, cancellationToken))
        {
            throw new InvalidOperationException($"メールアドレス '{request.Email}' は既に使用されています。");
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.BirthDate = request.BirthDate;
        user.UpdatedAt = DateTime.UtcNow;

        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        return updatedUser.ToDto();
    }

    public async Task<List<UserDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var children = await _userRepository.GetChildrenByParentIdAsync(parentId, cancellationToken);
        return children.Select(c => c.ToDto()).ToList();
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"ユーザーID '{userId}' が見つかりません。");

        await _userRepository.DeleteAsync(userId, cancellationToken);
    }
}

/// <summary>
/// ユーザーエンティティをDTOに変換する拡張メソッド
/// </summary>
public static class UserExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            BirthDate = user.BirthDate,
            ParentId = user.ParentId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Profile = user.Profile?.ToDto()
        };
    }

    public static UserProfileDto ToDto(this UserProfile profile)
    {
        return new UserProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            AvatarUrl = profile.AvatarUrl,
            Theme = profile.Theme,
            Language = profile.Language,
            TimeZone = profile.TimeZone
        };
    }
}