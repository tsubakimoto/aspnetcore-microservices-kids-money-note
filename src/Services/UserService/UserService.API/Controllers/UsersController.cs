using Microsoft.AspNetCore.Mvc;
using UserService.Domain.DTOs;
using UserService.Domain.Services;
using KidsMoneyNote.Shared.CommonLibrary;

namespace UserService.API.Controllers;

/// <summary>
/// ユーザー管理用APIコントローラー
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserDomainService _userDomainService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserDomainService userDomainService, ILogger<UsersController> logger)
    {
        _userDomainService = userDomainService;
        _logger = logger;
    }

    /// <summary>
    /// ユーザー情報を取得します
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ユーザー情報</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userDomainService.GetUserAsync(userId, cancellationToken);
            
            if (user == null)
            {
                var notFoundResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "ユーザーが見つかりません。",
                    RequestId = HttpContext.TraceIdentifier
                };
                return NotFound(notFoundResponse);
            }

            var response = new ApiResponse<UserDto>
            {
                Data = user,
                Success = true,
                Message = "ユーザー情報を取得しました。",
                RequestId = HttpContext.TraceIdentifier
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ユーザー取得中にエラーが発生しました。UserId: {UserId}", userId);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "内部サーバーエラーが発生しました。",
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "INTERNAL_ERROR",
                    Message = "ユーザー情報の取得に失敗しました。"
                }
            };
            
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 新しいユーザーを作成します
    /// </summary>
    /// <param name="request">ユーザー作成リクエスト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>作成されたユーザー情報</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userDomainService.CreateUserAsync(request, cancellationToken);

            var response = new ApiResponse<UserDto>
            {
                Data = user,
                Success = true,
                Message = "ユーザーを作成しました。",
                RequestId = HttpContext.TraceIdentifier
            };

            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "ユーザー作成時のバリデーションエラー: {Message}", ex.Message);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "VALIDATION_ERROR",
                    Message = ex.Message
                }
            };
            
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ユーザー作成中にエラーが発生しました。");
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "内部サーバーエラーが発生しました。",
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "INTERNAL_ERROR",
                    Message = "ユーザーの作成に失敗しました。"
                }
            };
            
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// ユーザー情報を更新します
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="request">ユーザー更新リクエスト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>更新されたユーザー情報</returns>
    [HttpPut("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userDomainService.UpdateUserAsync(userId, request, cancellationToken);

            var response = new ApiResponse<UserDto>
            {
                Data = user,
                Success = true,
                Message = "ユーザー情報を更新しました。",
                RequestId = HttpContext.TraceIdentifier
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "ユーザー更新時のエラー: {Message}", ex.Message);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "VALIDATION_ERROR",
                    Message = ex.Message
                }
            };
            
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ユーザー更新中にエラーが発生しました。UserId: {UserId}", userId);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "内部サーバーエラーが発生しました。",
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "INTERNAL_ERROR",
                    Message = "ユーザー情報の更新に失敗しました。"
                }
            };
            
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// 親IDで子どもリストを取得します
    /// </summary>
    /// <param name="parentId">親ID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>子どもリスト</returns>
    [HttpGet("parent/{parentId}/children")]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildren(Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var children = await _userDomainService.GetChildrenAsync(parentId, cancellationToken);

            var response = new ApiResponse<List<UserDto>>
            {
                Data = children,
                Success = true,
                Message = "子どもリストを取得しました。",
                RequestId = HttpContext.TraceIdentifier
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "子どもリスト取得中にエラーが発生しました。ParentId: {ParentId}", parentId);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "内部サーバーエラーが発生しました。",
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "INTERNAL_ERROR",
                    Message = "子どもリストの取得に失敗しました。"
                }
            };
            
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// ユーザーを削除します（論理削除）
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>削除結果</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _userDomainService.DeleteUserAsync(userId, cancellationToken);

            var response = new ApiResponse<object>
            {
                Success = true,
                Message = "ユーザーを削除しました。",
                RequestId = HttpContext.TraceIdentifier
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "ユーザー削除時のエラー: {Message}", ex.Message);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            };
            
            return NotFound(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ユーザー削除中にエラーが発生しました。UserId: {UserId}", userId);
            
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "内部サーバーエラーが発生しました。",
                RequestId = HttpContext.TraceIdentifier,
                Error = new ErrorDetails
                {
                    Code = "INTERNAL_ERROR",
                    Message = "ユーザーの削除に失敗しました。"
                }
            };
            
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
}