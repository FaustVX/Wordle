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
        var word = game.Try(Input(game));
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
    Console.WriteLine("Press any key to restart a game");
    Console.ReadLine();
}

static void WriteHeader(Game game)
{
    Console.Write($"{game.WordLength} letters, {game.PossibleTries} tries, ");
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
    return new(length, tries);
}

static string Input(Game game)
{
    (var lineSpacing, Console.CursorLeft) = (Console.CursorLeft, 0);
    (var top, Console.CursorTop) = (Console.CursorTop, 0);
    var length = game.WordLength;
    var word = new char[length];
    var currentLength = 0;
    var maxLength = 0;

    WriteHeader(game);
    Console.SetCursorPosition(lineSpacing, top);

    foreach (var letter in game.PlacedLetters)
        if (letter is (char c, _))
            Write(c, WellPlacedColor);
        else
            Console.CursorLeft++;
    do
    {
        Console.CursorLeft = currentLength + lineSpacing;
        var letter = Console.ReadKey(intercept: true);
        switch (letter.Key)
        {
            case ConsoleKey.Backspace:
                if (currentLength > 0)
                {
                    currentLength--;
                    maxLength--;
                    Console.CursorLeft--;
                    Write(game.PlacedLetters[currentLength].wellPlaced ?? ' ', WellPlacedColor);
                }
                continue;
            case ConsoleKey.LeftArrow:
                if (currentLength > 0)
                {
                    currentLength--;
                    Console.CursorLeft--;
                }
                continue;
            case ConsoleKey.RightArrow:
                if (currentLength < maxLength)
                {
                    currentLength++;
                    Console.CursorLeft++;
                }
                continue;
            case ConsoleKey.Enter:
                if (currentLength == length)
                {
                    var isUsefulWord = false;
                    for (var i = 0; !isUsefulWord && i < word.Length; i++)
                    {
                        var l = word[i];
                        if (game.IsValidAtPos(l, i) is UnknownLetter or (ValidLetter { AlreadyWellPlacedLetter: false } and not WellPlacedLetter))
                            isUsefulWord = true;
                    }
                    if (!isUsefulWord)
                        continue;
                    Console.WriteLine();
                    return new(word);
                }
                continue;
        }

        if (currentLength >= length)
            continue;
        if (letter.KeyChar is not (>= 'a' and <= 'z'))
            continue;

        switch (game.IsValidAtPos(letter.KeyChar, currentLength))
        {
            case WellPlacedLetter { AlreadyWellPlacedLetter: true }:
                Write(letter.KeyChar, WellPlacedColor, AlreadyWellPlacedColor);
                break;
            case ValidLetter { AlreadyWellPlacedLetter: true }:
                Write(letter.KeyChar, ValidColor, AlreadyWellPlacedColor);
                break;
            case InvalidLetter { AlreadyWellPlacedLetter: true }:
                Write(letter.KeyChar, InvalidColor, AlreadyWellPlacedColor);
                break;
            case UnknownLetter { AlreadyWellPlacedLetter: true }:
                Write(letter.KeyChar, Console.ForegroundColor, AlreadyWellPlacedColor);
                break;
            case WellPlacedLetter:
                Write(letter.KeyChar, WellPlacedColor);
                break;
            case ValidLetter:
                Write(letter.KeyChar, ValidColor);
                break;
            case InvalidLetter:
                Write(letter.KeyChar, InvalidColor);
                break;
            case UnknownLetter:
                Write(letter.KeyChar, Console.ForegroundColor);
                break;
        }

        word[currentLength] = letter.KeyChar;
        currentLength++;
        maxLength++;
    } while (true);
}
