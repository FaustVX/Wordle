using Wordle.Core;
using static ConsoleMenu.Helpers;
using System.Diagnostics;
using static Wordle.App.Options;

var game = Start();
var placedLetters = new (char? wellPlaced, HashSet<char>? invalid)[game.WordLength];
var validLetters = new HashSet<char>();
var invalidLetters = new HashSet<char>();

Console.Clear();
Console.WriteLine($"{game.WordLength} letters, {game.PossibleTries} tries");

do
{
    Console.Write($"{game.RemainingTries - 1} > ");
    var word = game.Try(Input(placedLetters, validLetters, invalidLetters));
    if (word is null)
    {
        Console.WriteLine("Invalid word!");
        continue;
    }
    Console.CursorTop--;
    Console.Write($"{game.RemainingTries} > ");
    for (int i = 0; i < word.Length; i++)
    {
        var letter = word[i];
        switch (letter)
        {
            case { IsWellPlaced: true }:
                Write(letter.Char, WellPlacedColor);
                placedLetters[i].wellPlaced = letter.Char;
                validLetters.Add(letter.Char);
                break;
            case { IsValid: true }:
                Write(letter.Char, ValidColor);
                validLetters.Add(letter.Char);
                (placedLetters[i].invalid ??= new()).Add(letter.Char);
                break;
            default:
                Write(letter.Char, InvalidColor);
                invalidLetters.Add(letter.Char);
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

static Game Start()
{
    Console.WriteLine("Welcome to Wordle");

    var length = Menu("Select word length", Game.ValideWordLength.ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
    var tries = Menu("Select possible tries", Enumerable.Range(4, 7).ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
    return new Game(length, tries);
}

static string Input((char? wellPlaced, HashSet<char>? invalid)[] placedLetters, IReadOnlyCollection<char> validLetters, IReadOnlyCollection<char> invalidLetters)
{
    var lineSpacing = Console.CursorLeft;
    var length = placedLetters.Length;
    var word = new char[length];
    var currentLength = 0;
    var maxLength = 0;

    foreach (var letter in placedLetters)
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
                    Write(placedLetters[currentLength].wellPlaced ?? ' ', WellPlacedColor);
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
                    Console.WriteLine();
                    return new(word);
                }
                continue;
        }

        if (currentLength >= length)
            continue;
        if (letter.KeyChar is not (>= 'a' and <= 'z'))
            continue;

        if (letter.KeyChar == placedLetters[currentLength].wellPlaced)
            Write(letter.KeyChar, WellPlacedColor);
        else if (placedLetters[currentLength].wellPlaced is char)
            if (placedLetters[currentLength].invalid?.Contains(letter.KeyChar) ?? false)
                Write(letter.KeyChar, InvalidColor, AlreadyWellPlacedColor);
            else if (validLetters.Contains(letter.KeyChar))
                Write(letter.KeyChar, ValidColor, AlreadyWellPlacedColor);
            else if (invalidLetters.Contains(letter.KeyChar))
                Write(letter.KeyChar, InvalidColor);
            else
                Write(letter.KeyChar, Console.ForegroundColor, AlreadyWellPlacedColor);
        else
            if (placedLetters[currentLength].invalid?.Contains(letter.KeyChar) ?? false)
                Write(letter.KeyChar, InvalidColor, AlreadyWellPlacedColor);
            else if (validLetters.Contains(letter.KeyChar))
                Write(letter.KeyChar, ValidColor);
            else if (invalidLetters.Contains(letter.KeyChar))
                Write(letter.KeyChar, InvalidColor);
            else
                Console.Write(letter.KeyChar);

        word[currentLength] = letter.KeyChar;
        currentLength++;
        maxLength++;
    } while (true);
}
