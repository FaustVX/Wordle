namespace Wordle.Core;

public abstract class BaseGame
{
    protected static readonly Dictionary<(int wordLength, int maxTries), (int totalGames, int[] scores)> _allScores = new();
    public int PossibleTries { get; }
    public int WordLength { get; }
    public IReadOnlyList<string> WordList { get; }
    public WordList CompleteWordList { get; }
    public int RemainingTries { get; protected set; }
    public bool IsRandomWord { get; }
    public (int totalGames, int[] scores) Scores
    {
        get => _allScores.TryGetValue((WordLength, PossibleTries), out var value)
                ? value
                : (_allScores[(WordLength, PossibleTries)] = (0, new int[PossibleTries]));

        set => _allScores[(WordLength, PossibleTries)] = value;
    }

    public BaseGame(int wordLength, int possibleTries, bool randomWord, WordList list)
    {
        WordLength = wordLength;
        RemainingTries = PossibleTries = possibleTries;
        IsRandomWord = randomWord;
        CompleteWordList = list;
        WordList = CompleteWordList[WordLength];
        Scores = (Scores.totalGames + 1, Scores.scores);
    }

    protected string SelectRandomWord(Random? rng = null)
        => WordList[(rng ?? new()).Next(WordList.Count)];

    public abstract BaseGame Recreate();

    public virtual bool IsPossibleWord(string word)
        => HasRemainingTries && IsValidWordLength(word) && IsWordInDictionary(word) && IsNotAllSameLetters(word);

    public bool HasRemainingTries => RemainingTries > 0;
    public bool IsValidWordLength(string word)
        => word.Length == WordLength;
    public bool IsWordInDictionary(string word)
        => IsRandomWord || WordList.Contains(word);
    public bool IsNotAllSameLetters(string word)
        => !IsRandomWord || word.Skip(1).Any(l => l != word[0]);
}
