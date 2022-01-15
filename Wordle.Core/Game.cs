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

    public Game(int wordLength, int possibleTries)
        => (WordLength, PossibleTries, RemainingTries, SelectedWord) = (wordLength, possibleTries, possibleTries, Words[wordLength][new Random().Next(Words[wordLength].Length)]);

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
                result[i] = new(word[i], CheckRemainingLetter(word[i], remainingLetters), false);

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
}
