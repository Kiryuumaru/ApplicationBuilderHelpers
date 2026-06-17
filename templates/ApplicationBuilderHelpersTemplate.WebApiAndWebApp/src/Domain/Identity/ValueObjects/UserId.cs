using Domain.Shared.Models;

namespace Domain.Identity.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a user.
/// </summary>
public class UserId : ValueObject
{
    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    protected UserId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new UserId with a randomly generated GUID.
    /// </summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a UserId from an existing GUID value.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <exception cref="ArgumentException">Thrown when value is empty.</exception>
    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty", nameof(value));
        }

        return new UserId(value);
    }

    public override string ToString() => Value.ToString();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
