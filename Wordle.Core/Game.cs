using System.Diagnostics;

namespace Wordle.Core;
public class Game
{
    public static readonly Dictionary<int, string[]> _words;

    static Game()
    {
        using var http = new HttpClient();
        _words = http.GetStringAsync(@"https://raw.githubusercontent.com/hbenbel/French-Dictionary/master/dictionary/dictionary.txt")
            .GetAwaiter()
            .GetResult()
            .Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Where([DebuggerStepThroughAttribute] static (word) => word.Length >= 3)
            .GroupBy([DebuggerStepThroughAttribute] static (word) => word.Length)
            .Select([DebuggerStepThroughAttribute] static (group) => (key: group.Key, words: group.ToArray()))
            .Where([DebuggerStepThroughAttribute] static (group) => group.words.Length > 10)
            .OrderBy([DebuggerStepThroughAttribute] static (group) => group.key)
            .ToDictionary([DebuggerStepThroughAttribute] static (group) => group.key, [DebuggerStepThroughAttribute] static (group) => group.words);
    }

    public static IEnumerable<int> ValideWordLength => _words.Keys;

    public int WordLength { get; }
    public int PossibleTries { get; }
    public string SelectedWord { get; }
    public int RemainingTries { get; private set; }

    public Game(int wordLength, int possibleTries)
        => (WordLength, PossibleTries, RemainingTries, SelectedWord) = (wordLength, possibleTries, possibleTries, _words[wordLength][new Random().Next(_words[wordLength].Length)]);

    public Letter[]? Try(string word)
    {
        if (RemainingTries <= 0 || word.Length != SelectedWord.Length || !_words[WordLength].Contains(word))
            return null;

        RemainingTries--;
        return word.Select((l, i) => new Letter(l, SelectedWord.Contains(l), SelectedWord[i] == l)).ToArray();
    }
}
