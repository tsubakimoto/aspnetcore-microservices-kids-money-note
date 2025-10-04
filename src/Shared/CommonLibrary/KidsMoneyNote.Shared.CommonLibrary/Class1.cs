namespace KidsMoneyNote.Shared.CommonLibrary;

/// <summary>
/// API レスポンス用の基底クラス
/// </summary>
/// <typeparam name="T">レスポンスデータの型</typeparam>
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public ErrorDetails? Error { get; set; }
}

/// <summary>
/// エラー詳細情報
/// </summary>
public class ErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ValidationError> Details { get; set; } = new();
}

/// <summary>
/// バリデーションエラー詳細
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// ページネーション情報
/// </summary>
public class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

/// <summary>
/// ページネーション付きレスポンス
/// </summary>
/// <typeparam name="T">アイテムの型</typeparam>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}
