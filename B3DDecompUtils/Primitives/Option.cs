using System.Diagnostics.CodeAnalysis;

namespace B3DDecompUtils.Primitives;

public readonly struct Option<T> where T : notnull
{
    private readonly bool hasValue;
    private readonly T value;

    private Option(bool hasValue, T value)
    {
        this.hasValue = hasValue;
        this.value = value;
    }

    public static Option<T> Some(T value) => new Option<T>(hasValue: true, value: value);
    public static Option<T> None => default;

    public static implicit operator Option<T>(Option.UnspecifiedNone _) => None;

    public bool IsSome => hasValue;
    public bool IsNone => !IsSome;

    public bool TryUnwrap([NotNullWhen(returnValue: true)] out T? outValue)
    {
        outValue = hasValue ? value : default;
        return hasValue;
    }
}

public static class Option
{
    public struct UnspecifiedNone {}
    public static UnspecifiedNone None => default;
    public static Option<T> Some<T>(T value) where T : notnull => Option<T>.Some(value: value);
    public static Option<T> FromNullable<T>(T? value) where T : notnull => value is null ? None : Some(value);
}
