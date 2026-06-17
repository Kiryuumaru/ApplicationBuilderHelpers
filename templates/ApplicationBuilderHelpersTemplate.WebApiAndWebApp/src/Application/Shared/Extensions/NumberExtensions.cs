namespace Application.Shared.Extensions;

public static class NumberExtensions
{
    public static long ToNonNegative(this long value)
    {
        return value < 0 ? 0 : value;
    }
}
