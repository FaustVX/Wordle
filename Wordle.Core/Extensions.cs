namespace Wordle.Core;

public static class Extensions
{
    public static IOrderedEnumerable<T> OrderByRandom<T>(this IEnumerable<T> source, Random? rng = null)
    {
        rng ??= new();
        return source.OrderBy(_ => rng.Next());
    }

    public static T[] AsArray<T>(this IEnumerable<T> source)
        => source is T[] a ? a : source.ToArray();
}
