using Wordle.Core;
using static ConsoleMenu.Helpers;
using System.Diagnostics;
using static Wordle.App.Options;
using System.IO.Compression;

for (var game = Start(); true; game = game.Recreate())
{
    Console.Clear();
    WriteHeader(game);
    Console.WriteLine();

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
        for (int i = 0; i < word.Length; i++)
        {
            var letter = word[i];
            switch (letter)
            {
                case { IsWellPlaced: true }:
                    Write(letter.Char, WellPlacedColor);
                    break;
                case { IsValid: true }:
                    Write(letter.Char, ValidColor);
                    break;
                default:
                    Write(letter.Char, InvalidColor);
                    break;
            }
        }
        Console.WriteLine();
        if (word.All([DebuggerStepThrough] static (letter) => letter.IsWellPlaced))
        {
            Console.WriteLine("Well played");
            break;
        }
    } while (game.RemainingTries > 0);

    Console.Write($"The word was ");
    (var foreground, Console.ForegroundColor) = (Console.ForegroundColor, ConsoleColor.Blue);
    Console.WriteLine(game.SelectedWord);
    if (!game.IsRandomWord)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Searching definition on '1mot.net'");
        (var minLength, Console.CursorLeft) = (Console.CursorLeft, 0);
        Console.ForegroundColor = ConsoleColor.Blue;
        foreach (var definition in await GetDefinition(game.SelectedWord))
            Console.WriteLine("- " + definition.PadRight(minLength, ' '));
    }
    Console.ForegroundColor = foreground;
    Console.WriteLine("Press any key to restart a game, or Esc to customize");
    if (Console.ReadKey().Key is ConsoleKey.Escape)
        game = Start();
}

static async Task<IEnumerable<string>> GetDefinition(string word)
{
    var http = new HttpClient();
    var response = await http.GetAsync($"https://1mot.net/{word}");

    using var stream = await response.Content.ReadAsStreamAsync();
    using var decompressed = new GZipStream(stream, CompressionMode.Decompress);
    using var reader = new StreamReader(decompressed);

    var content = reader.ReadToEnd();
    return GetDefinition(content);

    static IEnumerable<string> GetDefinition(string htmlContent)
    {
        var wikwikPos = htmlContent.IndexOf("WikWik.org");
        var defStartPos = htmlContent.IndexOf("<ul>", wikwikPos);
        var defEndPos = htmlContent.IndexOf("</ul>", defStartPos);
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
    Console.Write($"{game.WordLength} letters, {game.PossibleTries} tries, ");
    if (game.IsRandomWord)
        Console.Write("Random Word, ");
    for (var letter = 'a'; letter <= 'z'; letter++)
        Write(letter, game.PlacedLetters.Any(c => (c.wellPlaced ?? '\0') == letter) ? WellPlacedColor
                    : game.ValidLetters.Contains(letter) ? ValidColor
                    : game.InvalidLetters.Contains(letter) ? InvalidColor
                    : Console.ForegroundColor);
}

static Game Start()
{
    Console.WriteLine("Welcome to Wordle");

    var length = Menu("Select word length", Game.ValideWordLength.ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
    var tries = Menu("Select possible tries", Enumerable.Range(4, 7).ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
    var isRandom = Menu("Generate a random word ?", new[] { false, true }, [DebuggerStepThrough] static (b) => b ? "Yes" : "No");
    return new(length, tries, isRandom);
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
    do
    {
        WriteWord(word, hasChar, game, currentPosition);
        var letter = Console.ReadKey(intercept: true);
        switch (letter.Key)
        {
            case ConsoleKey.Backspace:
                if (currentPosition > 0)
                {
                    currentPosition--;
                    hasChar[currentPosition] = false;
                }
                continue;
            case ConsoleKey.Delete:
                if (currentPosition < length)
                    hasChar[currentPosition] = false;
                continue;
            case ConsoleKey.LeftArrow:
                if (currentPosition > 0)
                    currentPosition--;
                continue;
            case ConsoleKey.RightArrow:
                if (currentPosition < length)
                    currentPosition++;
                continue;
            case ConsoleKey.UpArrow:
                CycleLetter(word, hasChar, currentPosition, game, +1);
                continue;
            case ConsoleKey.DownArrow:
                CycleLetter(word, hasChar, currentPosition, game, -1);
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

        static void WriteWord(char[] word, bool[] hasChar, Game game, int currentPosition)
        {
            Console.CursorVisible = false;
            Console.CursorLeft = 0;
            Console.Write($"{game.RemainingTries - 1}: ");
            var lineSpacing = Console.CursorLeft;
            for (int i = 0; i < word.Length; i++)
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
                case ValidLetter { AlreadyWellPlacedLetter: true }:
                    Write(letter, ValidColor, AlreadyWellPlacedColor);
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
                case ValidLetter:
                    Write(letter, ValidColor);
                    break;
                case InvalidLetter:
                    Write(letter, InvalidColor);
                    break;
                case UnknownLetter:
                    Write(letter, Console.ForegroundColor);
                    break;
            }
        }
    } while (true);
}
