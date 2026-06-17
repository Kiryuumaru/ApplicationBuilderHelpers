using System;
using System.Collections.Generic;
using Domain.Shared.Models;

namespace Domain.Authorization.ValueObjects;

public sealed class RoleId : ValueObject
{
    public Guid Value { get; }

    public RoleId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("RoleId cannot be empty", nameof(value));
        }
        Value = value;
    }

    public static RoleId New() => new(Guid.NewGuid());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
