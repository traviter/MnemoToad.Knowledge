namespace MnemoToad.Knowledge.Data.Common;

public abstract record Result<T>
{
    private Result() { }

    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Message) : Result<T>;

    public static implicit operator Result<T>(T value) => new Success(value);
    public static implicit operator Result<T>(Error error) => new Failure(error.Message);
}
