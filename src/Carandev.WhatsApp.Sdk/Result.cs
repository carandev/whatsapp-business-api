using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk;

/// <summary>
/// Represents the non-throwing outcome of an operation that does not produce a value, such as a
/// delete call. Use <see cref="Success"/> and <see cref="Failure"/> to construct instances; the
/// default (parameterless) value of this struct is not a valid state — always create one via a
/// factory method.
/// </summary>
public readonly record struct Result
{
    private Result(bool isSuccess, WhatsAppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// <see langword="true"/> when the operation succeeded; otherwise <see langword="false"/>.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The error describing why the operation failed, populated only when
    /// <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </summary>
    public WhatsAppError? Error { get; }

    /// <summary>
    /// Creates a successful <see cref="Result"/>.
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed <see cref="Result"/> carrying the given error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    public static Result Failure(WhatsAppError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, error);
    }
}

/// <summary>
/// Represents the non-throwing outcome of an operation that produces a value of type
/// <typeparamref name="T"/> on success. Use <see cref="Success"/> and <see cref="Failure"/> to
/// construct instances; the default (parameterless) value of this struct is not a valid state —
/// always create one via a factory method.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public readonly record struct Result<T>
{
    private Result(bool isSuccess, T? value, WhatsAppError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// <see langword="true"/> when the operation succeeded; otherwise <see langword="false"/>.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The value produced by the operation, populated only when <see cref="IsSuccess"/> is
    /// <see langword="true"/>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The error describing why the operation failed, populated only when
    /// <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </summary>
    public WhatsAppError? Error { get; }

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/> wrapping the given value.
    /// </summary>
    /// <param name="value">The value produced by the operation.</param>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> carrying the given error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    public static Result<T> Failure(WhatsAppError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, default, error);
    }
}
