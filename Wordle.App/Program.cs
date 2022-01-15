using Wordle.Core;
using static ConsoleMenu.Helpers;
using System.Diagnostics;

var game = Start();
var wellPlacedLetters = new char?[game.WordLength];
var validLetters = new HashSet<char>();
var invalidLetters = new HashSet<char>();

Console.Clear();
Console.WriteLine($"{game.WordLength} letters, {game.PossibleTries} tries");

do
{
    var word = game.Try(Input(wellPlacedLetters, validLetters, invalidLetters));
    if (word is null)
    {
        Console.WriteLine("Wrong word!");
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
                Write(letter.Char.ToString(), ConsoleColor.Green);
                wellPlacedLetters[i] = letter.Char;
                validLetters.Add(letter.Char);
                break;
            case { IsValid: true }:
                Write(letter.Char.ToString(), ConsoleColor.DarkYellow);
                validLetters.Add(letter.Char);
                break;
            default:
                Write(letter.Char.ToString(), ConsoleColor.Red);
                invalidLetters.Add(letter.Char);
                break;
        }
    }
    Console.WriteLine();
    if (word.All([DebuggerStepThroughAttribute] static (letter) => letter.IsWellPlaced))
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

static string Input(char?[] wellPlacedLetters, IReadOnlyCollection<char> validLetters, IReadOnlyCollection<char> invalidLetters)
{
    var length = wellPlacedLetters.Length;
    var word = new char[length];
    var currentLength = 0;
    var maxLength = 0;
    do
    {
        Console.CursorLeft = currentLength;
        var letter = Console.ReadKey(intercept: true);
        switch (letter.Key)
        {
            case ConsoleKey.Backspace:
                if (currentLength > 0)
                {
                    currentLength--;
                    maxLength--;
                    Console.CursorLeft--;
                    Console.Write(' ');
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

        if (letter.KeyChar == wellPlacedLetters[currentLength])
            Write(letter.KeyChar.ToString(), ConsoleColor.Green);
        else if (validLetters.Contains(letter.KeyChar))
            Write(letter.KeyChar.ToString(), ConsoleColor.DarkYellow);
        else if (invalidLetters.Contains(letter.KeyChar))
            Write(letter.KeyChar.ToString(), ConsoleColor.Red);
        else
            Console.Write(letter.KeyChar);

        word[currentLength] = letter.KeyChar;
        currentLength++;
        maxLength++;
    } while (true);
}
