namespace Wordle.Core;

public class SquareWord : BaseGame
{

    public char[,] Grid { get; }
    public IReadOnlyList<string> SelectedWords { get; }
    public SquareWord(int wordLength, int possibleTries, WordList list)
        : base(wordLength, possibleTries, false, list)
    {
        Grid = new char[WordLength, WordLength];
        do
        {
            var selectedWords = new List<string>(WordLength);
            var firstVerticalWord = SelectRandomWord();
            for (var y = 0; y < WordLength; y++)
            {
                var word = SelectWordStartingWith(firstVerticalWord[y]);
                selectedWords.Add(word);
                for (var x = 0; x < wordLength; x++)
                    Grid[x, y] = word[x];
            }
            SelectedWords = selectedWords.AsReadOnly();
        } while (!IsValidGrid());

        bool IsValidGrid()
        {
            for (int x = 1; x < WordLength; x++)
            {
                var word = string.Concat(Enumerable.Range(0, wordLength).Select(y => Grid[x, y]));
                if (!WordList.Contains(word))
                    return false;
            }
            return true;
        }

        string SelectWordStartingWith(char startingLetter)
        {
            var rng = new Random();
            return WordList.Where(word => word[0] == startingLetter)
                .OrderBy(_ => rng.Next())
                .First();
        }
    }
}
