namespace Domain.Shared.Extensions;

public static class TypeExtensions
{
    public static bool IsNullable(this Type type)
    {
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }
        return true; // Reference types are always nullable
    }

    public static bool IsNullable<T>(this T? value) where T : struct
    {
        return value == null;
    }

    public static Type GetNullableUnderlyingType(this Type type)
    {
        return Nullable.GetUnderlyingType(type) is Type underlyingType ? underlyingType : type;
    }
}
