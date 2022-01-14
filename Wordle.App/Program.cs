using Wordle.Core;
using static ConsoleMenu.Helpers;
using System.Diagnostics;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Welcome to Wordle");

var length = Menu("Select word length", Game.ValideWordLength.ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
var tries = Menu("Select possible tries", Enumerable.Range(4, 10).ToArray(), [DebuggerStepThrough] static (i) => i.ToString());
var game = new Game(length, tries);

do
{
    var word = game.Try(Console.ReadLine()!);
    if (word is null)
        continue;
    Console.CursorTop--;
    Console.Write($"{game.PossibleTries - game.RemainingTries} > ");
    foreach (var letter in word)
    {
        Write(letter.Char.ToString(), letter switch
        {
            { IsValid: false } => ConsoleColor.White,
            { IsWellPlaced: false } => ConsoleColor.Yellow,
            { IsWellPlaced: true } => ConsoleColor.Green,
        });
    }
    Console.WriteLine();
    if (word.All([DebuggerStepThroughAttribute] static (letter) => letter.IsWellPlaced))
    {
        Console.WriteLine("Well played");
        break;
    }
} while (game.RemainingTries > 0);