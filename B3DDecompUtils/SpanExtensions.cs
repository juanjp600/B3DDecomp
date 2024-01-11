namespace B3DDecompUtils;

public static class SpanExtensions
{
    public static bool Any<T>(this ReadOnlySpan<T> span, Predicate<T> predicate)
    {
        foreach (var item in span)
        {
            if (predicate(item)) { return true; }
        }
        return false;
    }

    public static int Count<T>(this ReadOnlySpan<T> span, Predicate<T> predicate)
    {
        int counter = 0;
        foreach (var item in span)
        {
            if (predicate(item)) { counter++; }
        }
        return counter;
    }
}