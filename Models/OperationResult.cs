namespace CapstoneProject.Models;

public class OperationResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;

    public static OperationResult Success()
    {
        return new OperationResult { IsSuccess = true };
    }

    public static OperationResult Failure(string message)
    {
        return new OperationResult { Message = message };
    }
}

public class OperationResult<T>
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Value { get; init; }

    public static OperationResult<T> Success(T value)
    {
        return new OperationResult<T> { IsSuccess = true, Value = value };
    }

    public static OperationResult<T> Failure(string message)
    {
        return new OperationResult<T> { Message = message };
    }
}
