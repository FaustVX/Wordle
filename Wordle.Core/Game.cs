using System.Diagnostics;

namespace Wordle.Core;

public class Game : BaseGame
{
    public static IReadOnlyDictionary<(int wordLength, int maxTries), (int totalGames, int[] scores)> AllScores => _allScores;

    public string SelectedWord { get; }
    public (char? wellPlaced, HashSet<char>? invalid)[] PlacedLetters { get; }

    private readonly HashSet<char> validLetters = new();
    public IReadOnlyCollection<char> ValidLetters => validLetters;

    private readonly HashSet<char> invalidLetters = new();
    public IReadOnlyCollection<char> InvalidLetters => invalidLetters;

    public Game(int wordLength, int possibleTries, bool randomWord, WordList list)
        : base(wordLength, possibleTries, randomWord, list)
    {
        PlacedLetters = new (char? wellPlaced, HashSet<char>? invalid)[WordLength];
        SelectedWord = IsRandomWord
            ? string.Concat(Enumerable.Repeat(new Random(), wordLength).Select(static rng => (char)rng.Next('a', 'z' + 1)))
            : SelectRandomWord();
        Scores = (Scores.totalGames + 1, Scores.scores);
    }

    public Game Recreate()
        => new(WordLength, PossibleTries, IsRandomWord, CompleteWordList);

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
