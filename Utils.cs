namespace Coriander;

public static class Utils
{
    public static IEnumerable<IEnumerable<T>> GenerateCombinations<T>(IEnumerable<T> elements, int length)
    {
        return elements
            .SelectMany((element, index) =>
                GenerateCombinations(elements.Skip(index + 1), length - 1)
                    .Select(combination => new[] { element }.Concat(combination)))
            .Where(combination => combination.Count() == length);
    }
}