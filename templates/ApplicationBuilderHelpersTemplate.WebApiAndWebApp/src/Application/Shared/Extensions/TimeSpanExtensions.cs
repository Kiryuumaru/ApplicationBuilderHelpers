namespace Application.Shared.Extensions;

public static class TimeSpanExtensions
{
    public static CancellationTokenSource ToCancellationTokenSource(this TimeSpan value)
    {
        return new(value);
    }

    public static CancellationToken ToCancellationToken(this TimeSpan value)
    {
        return ToCancellationTokenSource(value).Token;
    }
}
