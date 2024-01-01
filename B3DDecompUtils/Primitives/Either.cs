using System.Diagnostics.CodeAnalysis;

namespace B3DDecompUtils.Primitives;

public readonly struct Either<T1, T2>
    where T1 : notnull
    where T2 : notnull
{
    private readonly Option<T1> option1;
    private readonly Option<T2> option2;

    private Either(Option<T1> option1, Option<T2> option2)
    {
        this.option1 = option1;
        this.option2 = option2;
    }

    public static Either<T1, T2> From1(T1 value1)
        => new Either<T1, T2>(Option.Some(value1), Option.None);

    public static Either<T1, T2> From2(T2 value2)
        => new Either<T1, T2>(Option.None, Option.Some(value2));

    public bool TryUnwrap1([NotNullWhen(returnValue: true)] out T1? value1)
        => option1.TryUnwrap(out value1);

    public bool TryUnwrap2([NotNullWhen(returnValue: true)] out T2? value2)
        => option2.TryUnwrap(out value2);
}
