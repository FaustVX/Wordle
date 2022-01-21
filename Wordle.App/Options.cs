namespace Wordle.App;

public static class Options
{
    public static ConsoleColor WellPlacedColor => ConsoleColor.Green;
    public static ConsoleColor WronglyPlacedColor => ConsoleColor.Yellow;
    public static ConsoleColor InvalidColor => ConsoleColor.Red;
    public static ConsoleColor AlreadyWellPlacedColor => ConsoleColor.DarkGray;

    public static void Write(char c, ConsoleColor foreground)
    {
        (var fore, Console.ForegroundColor) = (Console.ForegroundColor, foreground);
        Console.Write(c);
        Console.ForegroundColor = fore;
    }

    public static void Write(char c, ConsoleColor foreground, ConsoleColor background)
    {
        (var fore, Console.ForegroundColor) = (Console.ForegroundColor, foreground);
        (var back, Console.BackgroundColor) = (Console.BackgroundColor, background);
        Console.Write(c);
        (Console.ForegroundColor, Console.BackgroundColor) = (fore, back);
    }
}