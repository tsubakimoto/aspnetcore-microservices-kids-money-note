using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
/// ユーザーリポジトリインターフェイス
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// ユーザーIDでユーザーを取得
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ユーザーエンティティ</returns>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// メールアドレスでユーザーを取得
    /// </summary>
    /// <param name="email">メールアドレス</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ユーザーエンティティ</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 親IDで子どもリストを取得
    /// </summary>
    /// <param name="parentId">親ID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>子どもエンティティリスト</returns>
    Task<List<User>> GetChildrenByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを作成
    /// </summary>
    /// <param name="user">ユーザーエンティティ</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>作成されたユーザーエンティティ</returns>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを更新
    /// </summary>
    /// <param name="user">ユーザーエンティティ</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>更新されたユーザーエンティティ</returns>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーを削除（論理削除）
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// メールアドレスの重複チェック
    /// </summary>
    /// <param name="email">メールアドレス</param>
    /// <param name="excludeUserId">除外するユーザーID（更新時など）</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>重複している場合はtrue</returns>
    Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}