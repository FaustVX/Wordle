using Wordle.Core;
using System.Diagnostics;
using System.IO.Compression;
using static ConsoleMenu.Helpers;
using static Wordle.App.Options;
using Cocona;
using System.ComponentModel.DataAnnotations;

CoconaLiteApp.Run(Run);

static async Task Run(
    [Argument, Range(3, int.MaxValue)] int wordLength = 5,
    [Argument, Range(4, 10)] int tries = 6,
    [Option('r')] bool isRandom = false,
    [Argument, IsValidLanguage] string language = "fr")
{
    for (var (game, customize) = (new Game(wordLength, tries, isRandom, false, WordList.WordLists[language]), false); true; (game, customize) = (customize ? Start(game) : game.Recreate(), false))
    {
        Console.Clear();
        WriteHeader(game);
        Console.WriteLine();

        var found = false;
        do
        {
            var input = Input(game);
            if (input is null)
                break;
            var word = game.Try(input);
            if (word is null)
            {
                Console.WriteLine("Invalid word!");
                continue;
            }
            Console.CursorTop--;
            Console.Write($"{game.RemainingTries}> ");
            for (var i = 0; i < word.Length; i++)
            {
                var letter = word[i];
                switch (letter)
                {
                    case { IsWellPlaced: true }:
                        Write(letter.Char, WellPlacedColor);
                        break;
                    case { IsValid: true }:
                        Write(letter.Char, WronglyPlacedColor);
                        break;
                    default:
                        Write(letter.Char, InvalidColor);
                        break;
                }
            }
            Console.WriteLine();
            if (word.All([DebuggerStepThrough] static (letter) => letter.IsWellPlaced))
            {
                found = true;
                Console.WriteLine("Well played");
                break;
            }
        } while (game.RemainingTries > 0);

        Console.Write($"The word was ");
        (var foreground, Console.ForegroundColor) = (Console.ForegroundColor, SelectedWordColor);
        Console.WriteLine(game.SelectedWord);
        if (!game.IsRandomWord)
        {
            Console.ForegroundColor = SearchingColor;
            Console.Write("Searching definition on '1mot.net'");
            (var minLength, Console.CursorLeft) = (Console.CursorLeft, 0);
            Console.ForegroundColor = DefinitionColor;
            foreach (var definition in await GetDefinition(game.SelectedWord))
                Console.WriteLine("- " + definition.PadRight(minLength, ' '));
        }
        Console.ForegroundColor = foreground;

        PrintScores(game, found);

        Console.WriteLine("Press any key to restart a game, or Esc to customize");
        if (Console.ReadKey().Key is ConsoleKey.Escape)
            customize = true;
    }
}

static void PrintScores(Game game, bool found)
{
    Console.WriteLine("High Scores:");
    var (totalGames, scores) = game.Scores;
    for (var i = scores.Length - 1; i >= 0; i--)
    {
        var score = scores[i];
        totalGames -= score;
        if (score is not 0)
            Write($"{i} : {score}\n", found && game.RemainingTries == i ? CurrentHighScore : AllHighScore);
    }
    if (totalGames is not 0)
        Write($"X : {totalGames}\n", !found ? CurrentHighScore : AllHighScore);
}

static async Task<IEnumerable<string>> GetDefinition(string word)
{
    var http = new HttpClient();
    var response = await http.GetAsync($"https://1mot.net/{word}");

    if (!response.IsSuccessStatusCode)
        return Enumerable.Empty<string>();

    using var stream = await response.Content.ReadAsStreamAsync();
    using var decompressed = new GZipStream(stream, CompressionMode.Decompress);
    using var reader = new StreamReader(decompressed);

    var content = reader.ReadToEnd();
    return GetDefinition(content);

    static IEnumerable<string> GetDefinition(string htmlContent)
    {
        var wikwikPos = htmlContent.IndexOf("WikWik.org");
        if (wikwikPos < 0)
            yield break;
        var defStartPos = htmlContent.IndexOf("<ul>", wikwikPos);
        if (defStartPos < 0)
            yield break;
        var defEndPos = htmlContent.IndexOf("</ul>", defStartPos);
        if (defEndPos < 0)
            yield break;
        for (var startPos = htmlContent.IndexOf("<li>", defStartPos); startPos < defEndPos && startPos != -1; startPos = htmlContent.IndexOf("<li>", startPos + 1))
        {
            var startDefPos = htmlContent.IndexOf("&nbsp;", startPos);
            var endPos = htmlContent.IndexOf("</li>", startPos);

            yield return htmlContent[htmlContent.IndexOf(' ', startDefPos)..endPos].Trim();
        }
    }
}

static void WriteHeader(Game game)
{
    Console.Write($"{game.WordLength} letters, {game.PossibleTries} tries, {game.CompleteWordList.Name}, ");
    if (game.IsRandomWord)
        Console.Write("Random Word, ");
    for (var letter = 'a'; letter <= 'z'; letter++)
        Write(letter, game.PlacedLetters.Any(c => (c.wellPlaced ?? '\0') == letter) ? WellPlacedColor
                    : game.ValidLetters.Contains(letter) ? WronglyPlacedColor
                    : game.InvalidLetters.Contains(letter) ? InvalidColor
                    : Console.ForegroundColor);
}

static Game Start(Game previous)
{
    Console.WriteLine("Welcome to Wordle");

    var lists = WordList.WordLists.Values switch
    {
        IList<WordList> list => list,
        { } list => list.ToList(),
    };

    var length = Menu("Select word length", previous.CompleteWordList.ValidWordLength.ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
    var tries = Menu("Select possible tries", Enumerable.Range(4, 7).ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
    var isRandom = Menu("Generate a random word ?", new[] { false, true }, [DebuggerStepThrough] static (b) => b ? "Yes" : "No");
    var language = Menu("Select language", lists, wl => wl.Name);
    var knownLetters = Menu("Use known letters ?", new[] { false, true }, [DebuggerStepThrough] static (b) => b ? "Yes" : "No");
    return new(length, tries, isRandom, knownLetters, previous.CompleteWordList);
}

static string? Input(Game game)
{
    (var top, Console.CursorTop) = (Console.CursorTop, 0);
    var length = game.WordLength;
    var word = new char[length];
    var hasChar = new bool[length];
    var currentPosition = 0;

    WriteHeader(game);
    Console.SetCursorPosition(0, top);
    var hasChanged = true;
    do
    {
        WriteWord(word, hasChar, game, currentPosition, hasChanged);
        hasChanged = false;
        var letter = Console.ReadKey(intercept: true);
        switch (letter.Key)
        {
            case ConsoleKey.Backspace:
                if (currentPosition > 0)
                {
                    currentPosition--;
                    hasChar[currentPosition] = false;
                    hasChanged = true;
                }
                continue;
            case ConsoleKey.Delete:
                if (currentPosition < length && hasChar[currentPosition])
                {
                    hasChar[currentPosition] = false;
                    hasChanged = true;
                }
                continue;
            case ConsoleKey.LeftArrow:
                if (currentPosition > 0)
                {
                    if (letter.Modifiers.HasFlag(ConsoleModifiers.Control))
                        currentPosition = 0;
                    else
                        currentPosition--;
                    hasChanged = true;
                }
                continue;
            case ConsoleKey.RightArrow:
                if (currentPosition < length)
                {
                    if (letter.Modifiers.HasFlag(ConsoleModifiers.Control))
                        currentPosition = length;
                    else
                        currentPosition++;
                    hasChanged = true;
                }
                continue;
            case ConsoleKey.UpArrow:
                if (currentPosition >= 0 && currentPosition < game.WordLength)
                {
                    CycleLetter(word, hasChar, currentPosition, game, +1);
                    hasChanged = true;
                }
                continue;
            case ConsoleKey.DownArrow:
                if (currentPosition >= 0 && currentPosition < game.WordLength)
                {
                    CycleLetter(word, hasChar, currentPosition, game, -1);
                    hasChanged = true;
                }
                continue;
            case ConsoleKey.Enter:
                if (game.IsPossibleWord(new(word)))
                {
                    var (isUsefulWord, containsSpace) = (false, false);
                    for (var i = 0; (!isUsefulWord || containsSpace) && i < word.Length; i++)
                    {
                        containsSpace = hasChar[i] is false;
                        isUsefulWord = game.IsValidAtPos(word[i], i) is UnknownLetter or WronglyPlacedLetter { AlreadyWellPlacedLetter: false };
                    }

                    if (!isUsefulWord || containsSpace)
                        continue;
                    Console.WriteLine();
                    return new(word);
                }
                continue;
            case ConsoleKey.Escape:
                Console.CursorLeft = 0;
                return null;
        }

        if (currentPosition >= length || letter.KeyChar is not (>= 'a' and <= 'z'))
            continue;

        word[currentPosition] = letter.KeyChar;
        hasChar[currentPosition] = true;
        currentPosition++;
        hasChanged = true;
    } while (true);

    static void CycleLetter(char[] word, bool[] hasChar, int currentPosition, Game game, int offset)
    {
        var startLetter = hasChar[currentPosition] ? word[currentPosition]
            : game.PlacedLetters[currentPosition].wellPlaced is char c ? c
            : offset > 0 ? 'z' : 'a';
        do
        {
            startLetter = (char)((startLetter + 26 + offset - 'a') % 26 + 'a');
        } while (game.IsValidAtPos(startLetter, currentPosition) is InvalidLetter);
        word[currentPosition] = startLetter;
        hasChar[currentPosition] = true;
    }

    static void WriteWord(char[] word, bool[] hasChar, Game game, int currentPosition, bool hasChanged)
    {
        if (!hasChanged)
            return;
        Console.CursorVisible = false;
        Console.CursorLeft = 0;
        Console.Write($"{game.RemainingTries - 1}: ");
        var lineSpacing = Console.CursorLeft;
        for (var i = 0; i < word.Length; i++)
        {
            if (!hasChar[i] && game.PlacedLetters[i].wellPlaced is char c)
            {
                word[i] = c;
                hasChar[i] = true;
            }
            if (hasChar[i])
                WriteChar(word[i], i, game);
            else
                Console.Write(' ');
        }
        Console.CursorLeft = lineSpacing + currentPosition;
        Console.CursorVisible = true;
    }

    static void WriteChar(char letter, int currentPosition, Game game)
    {
        switch (game.IsValidAtPos(letter, currentPosition))
        {
            case WellPlacedLetter { AlreadyWellPlacedLetter: true }:
                Write(letter, WellPlacedColor, AlreadyWellPlacedColor);
                break;
            case WronglyPlacedLetter { AlreadyWellPlacedLetter: true }:
                Write(letter, WronglyPlacedColor, AlreadyWellPlacedColor);
                break;
            case InvalidLetter { AlreadyWellPlacedLetter: true }:
                Write(letter, InvalidColor, AlreadyWellPlacedColor);
                break;
            case UnknownLetter { AlreadyWellPlacedLetter: true }:
                Write(letter, Console.ForegroundColor, AlreadyWellPlacedColor);
                break;
            case WellPlacedLetter:
                Write(letter, WellPlacedColor);
                break;
            case WronglyPlacedLetter:
                Write(letter, WronglyPlacedColor);
                break;
            case InvalidLetter:
                Write(letter, InvalidColor);
                break;
            case UnknownLetter:
                Write(letter, Console.ForegroundColor);
                break;
        }
    }
}
