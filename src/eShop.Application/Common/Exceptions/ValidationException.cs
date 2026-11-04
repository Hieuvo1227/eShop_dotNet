using FluentValidation.Results;

namespace eShop.Application.Common.Exceptions;

/// <summary>
/// Exception ném ra khi dữ liệu đầu vào của Command/Query không vượt qua kiểm tra của FluentValidation.
/// Gom toàn bộ lỗi theo từng tên trường (PropertyName).
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() 
        : base("Đã xảy ra một hoặc nhiều lỗi validation.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}

