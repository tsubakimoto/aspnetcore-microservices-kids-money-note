using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories;

/// <summary>
/// ユーザーリポジトリ実装
/// Entity Framework Coreを使用してデータアクセスを行います
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .Include(u => u.Parent)
            .Include(u => u.Children)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .Include(u => u.Parent)
            .Include(u => u.Children)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
    }

    public async Task<List<User>> GetChildrenByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .Where(u => u.ParentId == parentId && u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        
        // 作成されたユーザーを再取得（リレーションを含めて）
        return await GetByIdAsync(user.Id, cancellationToken) 
               ?? throw new InvalidOperationException("ユーザーの作成に失敗しました。");
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        
        // 更新されたユーザーを再取得（リレーションを含めて）
        return await GetByIdAsync(user.Id, cancellationToken) 
               ?? throw new InvalidOperationException("ユーザーの更新に失敗しました。");
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
        {
            // 論理削除
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.Email == email && u.IsActive);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }
}