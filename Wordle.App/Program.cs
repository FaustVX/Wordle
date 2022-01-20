using Wordle.Core;
using static ConsoleMenu.Helpers;
using System.Diagnostics;
using static Wordle.App.Options;

for (var game = Start(); true; game = game.Recreate())
{
    Console.Clear();
    WriteHeader(game);
    Console.WriteLine();

    do
    {
        Console.Write($"{game.RemainingTries - 1}: ");
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

    Console.WriteLine($"The word was {game.SelectedWord}");
    Console.WriteLine("Press any key to restart a game, or Esc to customize");
    if (Console.ReadKey().Key is ConsoleKey.Escape)
        game = Start();
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
    (var lineSpacing, Console.CursorLeft) = (Console.CursorLeft, 0);
    (var top, Console.CursorTop) = (Console.CursorTop, 0);
    var length = game.WordLength;
    var word = new char[length];
    var hasChar = new bool[length];
    var currentPosition = 0;

    WriteHeader(game);
    Console.SetCursorPosition(lineSpacing, top);

    foreach (var letter in game.PlacedLetters)
        if (letter is (char c, _))
            Write(c, WellPlacedColor);
        else
            Console.CursorLeft++;
    do
    {
        Console.CursorLeft = currentPosition + lineSpacing;
        var letter = Console.ReadKey(intercept: true);
        switch (letter.Key)
        {
            case ConsoleKey.Backspace:
                if (currentPosition > 0)
                {
                    currentPosition--;
                    hasChar[currentPosition] = false;
                    Console.CursorLeft--;
                    Write(game.PlacedLetters[currentPosition].wellPlaced ?? ' ', WellPlacedColor);
                    Console.CursorLeft--;
                }
                continue;
            case ConsoleKey.Delete:
                if (currentPosition < length)
                {
                    hasChar[currentPosition] = false;
                    Write(game.PlacedLetters[currentPosition].wellPlaced ?? ' ', WellPlacedColor);
                    Console.CursorLeft--;
                }
                continue;
            case ConsoleKey.LeftArrow:
                if (currentPosition > 0)
                {
                    currentPosition--;
                }
                continue;
            case ConsoleKey.RightArrow:
                if (currentPosition < length)
                {
                    currentPosition++;
                }
                continue;
            case ConsoleKey.UpArrow:
                {
                    var startLetter = hasChar[currentPosition] ? word[currentPosition]
                        : game.PlacedLetters[currentPosition].wellPlaced is char c ? c
                        : 'z';
                    do
                    {
                        startLetter = (char)((startLetter + 1 - 'a') % 26 + 'a');
                    } while (game.IsValidAtPos(startLetter, currentPosition) is InvalidLetter);
                    WriteChar(startLetter, ref currentPosition, game, word, hasChar);
                    currentPosition--;
                }
                continue;
            case ConsoleKey.DownArrow:
                {
                    var startLetter = hasChar[currentPosition] ? word[currentPosition]
                        : game.PlacedLetters[currentPosition].wellPlaced is char c ? c
                        : 'a';
                    do
                    {
                        startLetter = (char)((startLetter + (26 - 1) - 'a') % 26 + 'a');
                    } while (game.IsValidAtPos(startLetter, currentPosition) is InvalidLetter);
                    WriteChar(startLetter, ref currentPosition, game, word, hasChar);
                    currentPosition--;
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

        WriteChar(letter.KeyChar, ref currentPosition, game, word, hasChar);

        static void WriteChar(char letter, ref int currentPosition, Game game, char[] word, bool[] hasChar)
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

            word[currentPosition] = letter;
            hasChar[currentPosition] = true;
            currentPosition++;
        }
    } while (true);
}
