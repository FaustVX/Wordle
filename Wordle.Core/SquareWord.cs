using System.Collections.Immutable;

namespace Wordle.Core;

public class SquareWord : BaseGame
{

    public char[,] Grid { get; }
    public IReadOnlyList<string> SelectedWords { get; }
    public char[,] WellPlacedLetters { get; }
    public ImmutableHashSet<char>[] WronglyPlacedLetters { get; }
    public ImmutableHashSet<char> WrongLetters { get; private set; }
    public SquareWord(int wordLength, int possibleTries, WordList list, int? seed)
        : base(wordLength, possibleTries, false, list)
    {
        TryAddVertical(0, ImmutableList.Create<string>(), ImmutableList.Create<string>(), out var selectedHorizontalWords, seed is int i ? new(i) : new());

        SelectedWords = selectedHorizontalWords.ToList().AsReadOnly();
        WellPlacedLetters = new char[WordLength, WordLength];
        WronglyPlacedLetters = new ImmutableHashSet<char>[WordLength];
        WrongLetters = ImmutableHashSet<char>.Empty;
        Grid = new char[WordLength, WordLength];
        for (int y = 0; y < WordLength; y++)
        {
            WronglyPlacedLetters[y] = ImmutableHashSet<char>.Empty;
            for (int x = 0; x < WordLength; x++)
                Grid[x, y] = SelectedWords[y][x];
        }

        bool TryAddHorizontal(int i, ImmutableList<string> selectedVerticalWords, ImmutableList<string> selectedHorizontalWords, out ImmutableList<string> horizontalWords, Random rng, bool dryRun = false)
        {
            horizontalWords = selectedHorizontalWords;
            if (!dryRun && !TryAddVertical(i + 1, selectedVerticalWords, selectedHorizontalWords, out _, rng, dryRun: true))
                return false;

            var start = string.Concat(selectedVerticalWords.Select(w => w[i]));
            if (GetWordListOffsetAndLength(start) is not var (offset, length) || length is 0)
                return false;
            else if (dryRun)
                return true;
            foreach (var item in Enumerable.Range(offset, length).OrderByRandom(rng))
            {
                var word = WordList[item];
                var isOk = TryAddVertical(i + 1, selectedVerticalWords, selectedHorizontalWords.Add(word), out horizontalWords, rng);
                if (isOk)
                    return true;
            }
            return false;
        }

        bool TryAddVertical(int i, ImmutableList<string> selectedVerticalWords, ImmutableList<string> selectedHorizontalWords, out ImmutableList<string> horizontalWords, Random rng, bool dryRun = false)
        {
            horizontalWords = selectedHorizontalWords;
            if (i >= WordLength)
                return true;
            if (!dryRun && !TryAddHorizontal(i, selectedVerticalWords, selectedHorizontalWords, out _, rng, dryRun: true))
                return false;

            var start = string.Concat(selectedHorizontalWords.Select(w => w[i]));
            if (GetWordListOffsetAndLength(start) is not var (offset, length) || length is 0)
                return false;
            else if (dryRun)
                return true;
            foreach (var item in Enumerable.Range(offset, length).OrderByRandom(rng))
            {
                var word = WordList[item];
                var isOk = TryAddHorizontal(i, selectedVerticalWords.Add(word), selectedHorizontalWords, out horizontalWords, rng);
                if (isOk)
                    return true;
            }
            return false;
        }
    }

    public override SquareWord Recreate()
        => new(WordLength, PossibleTries, CompleteWordList, null);

    private (int offset, int length)? GetWordListOffsetAndLength(string start)
    {
        var range = GetBound<string>(WordList.AsArray(), w => w.AsSpan().StartsWith(start));
        return range is Range r ? r.GetOffsetAndLength(WordList.Count) : null;
    }

    private static Range? GetBound<T>(ReadOnlySpan<T> orderedSpan, Func<T, bool> isValid)
    {
        var (start, end, negate) = (0, 0, true);
        foreach (var item in orderedSpan)
        {
            if (negate)
                if (!isValid(item))
                    start++;
                else
                {
                    negate = false;
                    end = start + 1;
                }
            else
                if (isValid(item))
                    end++;
                else
                    break;
        }
        if (negate)
            return null;
        return start..end;
    }

    public bool Try(string word)
    {
        if (RemainingTries > 0 || word.Length != WordLength || !WordList.Contains(word))
            return false;
        foreach (var letter in word)
        {
            if (!SelectedWords.SelectMany(static w => w).Contains(letter))
                WrongLetters = WrongLetters.Add(letter);
        }
        for (int y = 0; y < WordLength; y++)
        {
            var letterCounts = word.GroupBy(static c => c)
                .ToDictionary(static g => g.Key, g => word.Count(c => c == g.Key));
            for (int x = 0; x < WordLength; x++)
                if (!WrongLetters.Contains(word[x]) && SelectedWords[y][x] == word[x] && letterCounts.TryGetValue(word[x], out var count) && count > 0)
                {
                    WronglyPlacedLetters[y] = WronglyPlacedLetters[y].Remove(word[x]);
                    letterCounts[word[x]]--;
                    WellPlacedLetters[x, y] = word[x];
                }
            for (int x = 0; x < WordLength; x++)
                if (!WrongLetters.Contains(word[x]) && SelectedWords[y].Contains(word[x]) && letterCounts.TryGetValue(word[x], out var count) && count > 0)
                {
                    letterCounts[word[x]]--;
                    WronglyPlacedLetters[y] = WronglyPlacedLetters[y].Add(word[x]);
                }
        }
        RemainingTries--;

        return true;
    }
}
