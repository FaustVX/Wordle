using Wordle.Core;
using System.Diagnostics;
using static ConsoleMenu.Helpers;
using Cocona;
using System.ComponentModel.DataAnnotations;

namespace Wordle.App;

static partial class Program
{
    private static void RunSquareword(
        [Argument, Range(3, int.MaxValue)] int wordLength = 5,
        [Argument, Range(4, 10)] int tries = 6,
        [Argument, IsValidLanguage] string language = "fr",
        int? seed = null)
    {
        Console.WriteLine("Generation may take a while");
        for (var (game, customize) = (new SquareWord(wordLength, tries, WordList.WordLists[language], seed), false); true; (game, customize) = (customize ? StartSquareword(game) : game.Recreate(), false))
        {
            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                foreach (var letter in game.WrongLetters)
                    Console.Write(letter);
                Console.ResetColor();
                Console.WriteLine();

                var (left, top) = (Console.CursorLeft, Console.CursorTop);
                for (var y = 0; y < game.WordLength; y++)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    for (var x = 0; x < game.WordLength; x++)
                        Console.Write(game.WellPlacedLetters[x, y] is not '\0' and var l ? l : ' ');
                    Console.ResetColor();
                    Console.Write("  ");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    foreach (var letter in game.WronglyPlacedLetters[y])
                        Console.Write(letter);
                    Console.ResetColor();
                    Console.SetCursorPosition(left, ++top);
                }

                if (game.RemainingTries <= 0)
                    break;

                (left, top) = (Console.CursorLeft, Console.CursorTop);
                for (var input = Console.ReadLine()!; !game.Try(input); input = Console.ReadLine()!)
                    Console.SetCursorPosition(left, top);
            } while (game.RemainingTries >= 0);
            Console.WriteLine("Game finished");
            Console.ReadLine();
        }
    }

    private static SquareWord StartSquareword(SquareWord previous)
    {
        Console.WriteLine("Welcome to Wordle");

        var lists = WordList.WordLists.Values switch
        {
            IList<WordList> list => list,
            { } list => list.ToList(),
        };

        var length = Menu("Select word length", previous.CompleteWordList.ValidWordLength.ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
        var tries = Menu("Select possible tries", Enumerable.Range(4, 7).ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
        var language = Menu("Select language", lists, wl => wl.Name);
        return new(length, tries, previous.CompleteWordList, null);
    }
}