namespace Wordle.Core;
public class Game
{
    public static readonly Dictionary<int, string[]> _words = new()
    {
        [5] = new[]
        {
            "assis",
            "ailes",
            "sales",
            "boire",
            "bisou",
            "salut",
        },
    };

    public static IEnumerable<int> ValideWordLength => _words.Keys;

    public int WordLength { get; }
    public int PossibleTries { get; }
    public string SelectedWord { get; }
    public int RemainingTries { get; private set; }

    public Game(int wordLength, int possibleTries)
        => (WordLength, PossibleTries, RemainingTries, SelectedWord) = (wordLength, possibleTries, possibleTries, _words[wordLength][new Random().Next(_words[wordLength].Length)]);

    public Letter[]? Try(string word)
    {
        if (RemainingTries <= 0 || word.Length != SelectedWord.Length)
            return null;

        RemainingTries--;
        return word.Select((l, i) => new Letter(l, SelectedWord.Contains(l), SelectedWord[i] == l)).ToArray();
    }
}
