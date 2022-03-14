using Cocona;

namespace Wordle.App;

static partial class Program
{
    private static async Task Main(string[]? args)
    {
        if (args?.Length is 0 or null)
            await RunWordle();
        else
        {
            var app = CoconaLiteApp.Create(args);
            app.AddCommand("wordle", RunWordle);
            app.AddCommand("squareword", RunSquareword);
            app.Run();
        }
    }
}