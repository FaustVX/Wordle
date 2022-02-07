using System.Diagnostics;

namespace Wordle.Core;

public class WordList
{
    public static WordList French { get; } = new(new(@"https://raw.githubusercontent.com/LouanBen/wordle-fr/main/mots.txt"));
    public static IReadOnlyDictionary<string, WordList> WordLists { get; } = new Dictionary<string, WordList>()
    {
        ["fr"] = French,
    };

    private readonly Dictionary<int, string[]> _wordLists;
    public IEnumerable<int> ValidWordLength => _wordLists.Keys;
    public IReadOnlyList<string> this[int length]
        => _wordLists[length];

    public WordList(Uri uri)
    {
        using var http = new HttpClient();
        _wordLists = http.GetStringAsync(uri)
            .GetAwaiter()
            .GetResult()
            .Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Where([DebuggerStepThrough] static (word) => word.Length >= 3)
            .Select([DebuggerStepThrough] static (word) => word.ToLower())
            .GroupBy([DebuggerStepThrough] static (word) => word.Length)
            .Select([DebuggerStepThrough] static (group) => (key: group.Key, words: group.ToArray()))
            .Where([DebuggerStepThrough] static (group) => group.words.Length > 10)
            .OrderBy([DebuggerStepThrough] static (group) => group.key)
            .ToDictionary([DebuggerStepThrough] static (group) => group.key, [DebuggerStepThrough] static (group) => group.words);
    }
}
