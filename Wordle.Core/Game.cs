using System.Diagnostics;

namespace Wordle.Core;
public class Game
{
    private static readonly Dictionary<int, string[]> _wordLists;
    public static IEnumerable<int> ValidWordLength => _wordLists.Keys;
    private static readonly Dictionary<(int wordLength, int maxTries), (int totalGames, int[] scores)> _allScores = new();
    public static IReadOnlyDictionary<(int wordLength, int maxTries), (int totalGames, int[] scores)> AllScores => _allScores;

    static Game()
    {
        using var http = new HttpClient();
        _wordLists = http.GetStringAsync(@"https://raw.githubusercontent.com/LouanBen/wordle-fr/main/mots.txt")
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

    public int WordLength { get; }
    public int PossibleTries { get; }
    public string SelectedWord { get; }
    public IReadOnlyList<string> WordList { get; }
    public int RemainingTries { get; private set; }
    public (char? wellPlaced, HashSet<char>? invalid)[] PlacedLetters { get; }
    public bool IsRandomWord { get; }

    private readonly HashSet<char> validLetters = new();
    public IReadOnlyCollection<char> ValidLetters => validLetters;

    private readonly HashSet<char> invalidLetters = new();
    public IReadOnlyCollection<char> InvalidLetters => invalidLetters;

    public (int totalGames, int[] scores) Scores
    {
        get => _allScores.TryGetValue((WordLength, PossibleTries), out var value)
                ? value
                : (_allScores[(WordLength, PossibleTries)] = (0, new int[PossibleTries]));

        set => _allScores[(WordLength, PossibleTries)] = value;
    }

    public Game(int wordLength, int possibleTries, bool randomWord)
    {
        IsRandomWord = randomWord;
        WordLength = wordLength;
        PossibleTries = possibleTries;
        RemainingTries = PossibleTries;
        PlacedLetters = new (char? wellPlaced, HashSet<char>? invalid)[WordLength];
        WordList = _wordLists[WordLength];
        SelectedWord = IsRandomWord
            ? string.Concat(Enumerable.Repeat(new Random(), wordLength).Select(static rng => (char)rng.Next('a', 'z' + 1)))
            : WordList[new Random().Next(WordList.Count)];
        Scores = (Scores.totalGames + 1, Scores.scores);
    }

    public Game Recreate()
        => new(WordLength, PossibleTries, IsRandomWord);

    public bool IsPossibleWord(string word)
        => HasRemainingTries && IsValidWordLength(word) && IsWordInDictionary(word) && IsNotAllSameLetters(word);
    public bool HasRemainingTries => RemainingTries > 0;
    public bool IsValidWordLength(string word)
        => word.Length == WordLength;
    public bool IsWordInDictionary(string word)
        => IsRandomWord || WordList.Contains(word);
    public bool IsNotAllSameLetters(string word)
        => !IsRandomWord || word.Skip(1).Any([DebuggerStepThrough] (l) => l != word[0]);

    public Letter[]? Try(string word)
    {
        if (!IsPossibleWord(word))
            return null;

        RemainingTries--;
        var remainingLetters = SelectedWord.GroupBy(static l => l).ToDictionary(static g => g.Key, static g => g.Count());
        var result = new Letter[SelectedWord.Length];
        for (var i = 0; i < result.Length; i++)
            if (word[i] == SelectedWord[i])
            {
                result[i] = new(word[i], true, true);
                remainingLetters[word[i]]--;
            }

        for (var i = 0; i < result.Length; i++)
            if (word[i] != SelectedWord[i])
                result[i] = new(word[i], CheckRemainingLetters(word[i], remainingLetters), false);

        for (var i = 0; i < result.Length; i++)
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
                    if (!remainingLetters.ContainsKey(c))
                        invalidLetters.Add(c);
                    (PlacedLetters[i].invalid ??= new()).Add(c);
                    break;
            }
        if (result.All(static l => l.IsWellPlaced))
            Scores.scores[RemainingTries]++;

        return result;

        static bool CheckRemainingLetters(char l, Dictionary<char, int> remainingLetters)
        {
            if (remainingLetters.TryGetValue(l, out var remaining) && remaining > 0)
            {
                remainingLetters[l]--;
                return true;
            }
            return false;
        }
    }

    public LetterPlacement IsValidAtPos(char letter, int index)
    {
        if (letter == PlacedLetters[index].wellPlaced)
            return new WellPlacedLetter();
        else if (PlacedLetters[index].invalid?.Contains(letter) ?? false)
            return new InvalidLetter() { AlreadyWellPlacedLetter = true };
        else if (InvalidLetters.Contains(letter))
            return new InvalidLetter();
        else if (PlacedLetters[index].wellPlaced is char)
            if (ValidLetters.Contains(letter))
                return new WronglyPlacedLetter() { AlreadyWellPlacedLetter = true };
            else
                return new UnknownLetter() { AlreadyWellPlacedLetter = true };
        else
        {
            if (ValidLetters.Contains(letter))
                return new WronglyPlacedLetter();
            else
                return new UnknownLetter();
        }
    }
}
