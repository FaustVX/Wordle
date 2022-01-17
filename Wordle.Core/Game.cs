using System.Diagnostics;

namespace Wordle.Core;
public class Game
{
    private static readonly Dictionary<int, string[]> _wordLists;
    public static IEnumerable<int> ValideWordLength => _wordLists.Keys;

    static Game()
    {
        using var http = new HttpClient();
        _wordLists = http.GetStringAsync(@"https://raw.githubusercontent.com/hbenbel/French-Dictionary/master/dictionary/dictionary.txt")
            .GetAwaiter()
            .GetResult()
            .Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Where([DebuggerStepThrough] static (word) => word.Length >= 3)
            .Where([DebuggerStepThrough] static (word) => !word.Contains('-'))
            .GroupBy([DebuggerStepThrough] static (word) => word.Length)
            .Select([DebuggerStepThrough] static (group) => (key: group.Key, words: group.ToArray()))
            .Where([DebuggerStepThrough] static (group) => group.words.Length > 10)
            .OrderBy([DebuggerStepThrough] static (group) => group.key)
            .ToDictionary([DebuggerStepThrough] static (group) => group.key, [DebuggerStepThrough] static (group) => group.words);
    }

    public int WordLength { get; }
    public int PossibleTries { get; }
    public string SelectedWord { get; }
    public string SanitizedWord { get; }
    public IReadOnlyList<string> WordList { get; }
    public int RemainingTries { get; private set; }
    public (char? wellPlaced, HashSet<char>? invalid)[] PlacedLetters { get; }
    public bool IsRandomWord { get; }

    private readonly HashSet<char> validLetters = new();
    public IReadOnlyCollection<char> ValidLetters => validLetters;

    private readonly HashSet<char> invalidLetters = new();
    public IReadOnlyCollection<char> InvalidLetters => invalidLetters;

    public Game(int wordLength, int possibleTries, bool randomWord)
    {
        IsRandomWord = randomWord;
        WordLength = wordLength;
        PossibleTries = possibleTries;
        RemainingTries = PossibleTries;
        PlacedLetters = new (char? wellPlaced, HashSet<char>? invalid)[WordLength];
        WordList = _wordLists[WordLength];
        SelectedWord = IsRandomWord
            ? string.Concat(Enumerable.Repeat(new Random(), wordLength).Select(rng => (char)rng.Next('a', 'z' + 1)))
            : WordList[new Random().Next(WordList.Count)];
        SanitizedWord = Sanitize(SelectedWord);
    }

    public Game Recreate()
        => new(WordLength, PossibleTries, IsRandomWord);

    public bool IsPossibleWord(string word)
        => RemainingTries > 0 && word.Length == SanitizedWord.Length && (IsRandomWord || WordList.Select(Sanitize).Contains(word)) && word.Skip(1).Any(l => l != word[0]);

    public Letter[]? Try(string word)
    {
        if (!IsPossibleWord(Sanitize(word)))
            return null;

        RemainingTries--;
        var remainingLetters = SanitizedWord.GroupBy(static l => l).ToDictionary(g => g.Key, g => g.Count());
        var result = new Letter[SanitizedWord.Length];
        for (int i = 0; i < result.Length; i++)
            if (word[i] == SanitizedWord[i])
            {
                result[i] = new(word[i], true, true);
                remainingLetters[word[i]]--;
            }

        for (int i = 0; i < result.Length; i++)
            if (word[i] != SanitizedWord[i])
                result[i] = new(word[i], CheckRemainingLetter(word[i], remainingLetters), false);

        for (int i = 0; i < result.Length; i++)
            switch (result[i])
            {
                case { IsWellPlaced: true, Char: var c }:
                    PlacedLetters[i].wellPlaced = c;
                    validLetters.Add(c);
                    break;
                case { IsValid: true, Char: var c }:
                    validLetters.Add(c);
                    (PlacedLetters[i].invalid ??= new()).Add(c);
                    break;
                case { Char: var c }:
                    invalidLetters.Add(c);
                    break;
            }

        return result;

        static bool CheckRemainingLetter(char l, Dictionary<char, int> remainingLetters)
        {
            if (remainingLetters.TryGetValue(l, out var remaining) && remaining > 0)
            {
                remainingLetters[l]--;
                return true;
            }
            return false;
        }
    }

    private static string Sanitize(string word)
        => word.ToLower()
            .Replace('â', 'a')
            .Replace('à', 'a')
            .Replace('ä', 'a')
            .Replace('é', 'e')
            .Replace('è', 'e')
            .Replace('ê', 'e')
            .Replace('ë', 'e')
            .Replace('ï', 'i')
            .Replace('î', 'i')
            .Replace('ô', 'o')
            .Replace('ö', 'o')
            .Replace('û', 'u')
            .Replace('ü', 'u')
            .Replace('ù', 'u')
            .Replace('ç', 'c');

    public LetterPlacement IsValidAtPos(char letter, int index)
    {
        if (letter == PlacedLetters[index].wellPlaced)
            return new WellPlacedLetter();
        else if (PlacedLetters[index].invalid?.Contains(letter) ?? false)
            return new InvalidLetter() { AlreadyWellPlacedLetter = true };
        else if (PlacedLetters[index].wellPlaced is char)
            if (ValidLetters.Contains(letter))
                return new ValidLetter() { AlreadyWellPlacedLetter = true };
            else if (InvalidLetters.Contains(letter))
                return new InvalidLetter();
            else
                return new UnknownLetter() { AlreadyWellPlacedLetter = true };
        else
            if (ValidLetters.Contains(letter))
            return new ValidLetter();
        else if (InvalidLetters.Contains(letter))
            return new InvalidLetter();
        else
            return new UnknownLetter();
    }
}
