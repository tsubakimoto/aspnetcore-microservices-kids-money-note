using FluentValidation;
using UserService.Domain.DTOs;

namespace UserService.API.Validators;

/// <summary>
/// ユーザー作成リクエストのバリデーター
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("名前は必須です。")
            .MaximumLength(100)
            .WithMessage("名前は100文字以内で入力してください。");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("メールアドレスは必須です。")
            .EmailAddress()
            .WithMessage("有効なメールアドレスを入力してください。")
            .MaximumLength(255)
            .WithMessage("メールアドレスは255文字以内で入力してください。");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("ロールは必須です。")
            .Must(role => role == "Child" || role == "Parent")
            .WithMessage("ロールは 'Child' または 'Parent' である必要があります。");

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .WithMessage("生年月日は必須です。")
            .LessThan(DateTime.Today)
            .WithMessage("生年月日は今日より前の日付である必要があります。");

        // 子どもの場合はParentIdが必須
        RuleFor(x => x.ParentId)
            .NotEmpty()
            .When(x => x.Role == "Child")
            .WithMessage("子どもユーザーには親IDが必要です。");

        // 親の場合はParentIdは不要
        RuleFor(x => x.ParentId)
            .Empty()
            .When(x => x.Role == "Parent")
            .WithMessage("親ユーザーに親IDは設定できません。");
    }
}

/// <summary>
/// ユーザー更新リクエストのバリデーター
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("名前は必須です。")
            .MaximumLength(100)
            .WithMessage("名前は100文字以内で入力してください。");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("メールアドレスは必須です。")
            .EmailAddress()
            .WithMessage("有効なメールアドレスを入力してください。")
            .MaximumLength(255)
            .WithMessage("メールアドレスは255文字以内で入力してください。");

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .WithMessage("生年月日は必須です。")
            .LessThan(DateTime.Today)
            .WithMessage("生年月日は今日より前の日付である必要があります。");
    }
}