using System.Collections.Immutable;

namespace Wordle.Core;

public class SquareWord : BaseGame
{

    public char[,] Grid { get; }
    public IReadOnlyList<string> SelectedWords { get; }
    public SquareWord(int wordLength, int possibleTries, WordList list)
        : base(wordLength, possibleTries, false, list)
    {
        do
        {
            var rng = new Random();
            var firstVerticalWord = SelectRandomWord(rng)!;
            var isOk = TryAddHorizontal(0, ImmutableList.Create(firstVerticalWord), ImmutableList.Create<string>(), out var selectedHorizontalWords, rng);
            if (isOk)
            {
                SelectedWords = selectedHorizontalWords.ToList().AsReadOnly();
                Grid = new char[WordLength, WordLength];
                for (int y = 0; y < WordLength; y++)
                    for (int x = 0; x < WordLength; x++)
                        Grid[x, y] = SelectedWords[y][x];
                break;
            }
        } while (true);

        bool TryAddHorizontal(int i, ImmutableList<string> selectedVerticalWords, ImmutableList<string> selectedHorizontalWords, out ImmutableList<string> horizontalWords, Random rng, bool dryRun = false)
        {
            horizontalWords = selectedHorizontalWords;
            if (!dryRun && !TryAddVertical(i, selectedVerticalWords, selectedHorizontalWords, out _, rng, dryRun: true))
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
}
