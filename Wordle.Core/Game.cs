using System.Diagnostics;

namespace Wordle.Core;
public class Game
{
    public static readonly Dictionary<int, string[]> Words;

    static Game()
    {
        using var http = new HttpClient();
        Words = http.GetStringAsync(@"https://raw.githubusercontent.com/hbenbel/French-Dictionary/master/dictionary/dictionary.txt")
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

    public static IEnumerable<int> ValideWordLength => Words.Keys;

    public int WordLength { get; }
    public int PossibleTries { get; }
    public string SelectedWord { get; }
    public int RemainingTries { get; private set; }

    public (char? wellPlaced, HashSet<char>? invalid)[] PlacedLetters { get; }

    private readonly HashSet<char> validLetters = new();
    public IReadOnlyCollection<char> ValidLetters => validLetters;

    private readonly HashSet<char> invalidLetters = new();
    public IReadOnlyCollection<char> InvalidLetters => invalidLetters;

    public Game(int wordLength, int possibleTries)
    {
        WordLength = wordLength;
        PossibleTries = possibleTries;
        RemainingTries = PossibleTries;
        PlacedLetters = new (char? wellPlaced, HashSet<char>? invalid)[WordLength];
        SelectedWord = Words[WordLength][new Random().Next(Words[WordLength].Length)];
    }

    public Game Recreate()
        => new(WordLength, PossibleTries);

    public Letter[]? Try(string word)
    {
        var sanitizedWord = Sanitize(SelectedWord);
        if (RemainingTries <= 0 || word.Length != sanitizedWord.Length || !Words[WordLength].Select(Sanitize).Contains(word))
            return null;

        RemainingTries--;
        var remainingLetters = sanitizedWord.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());
        var result = new Letter[sanitizedWord.Length];
        for (int i = 0; i < result.Length; i++)
            if (word[i] == sanitizedWord[i])
            {
                result[i] = new(word[i], true, true);
                remainingLetters[word[i]]--;
            }

        for (int i = 0; i < result.Length; i++)
            if (word[i] != sanitizedWord[i])
            {
                result[i] = new(word[i], CheckRemainingLetter(word[i], remainingLetters), false);
            }

        for (int i = 0; i < result.Length; i++)
        {
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

        static string Sanitize(string word)
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
    }

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
