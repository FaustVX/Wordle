namespace Wordle.Core;

public readonly struct Letter
{
    public readonly char Char;
    public readonly bool IsValid, IsWellPlaced;

    public Letter(char @char, bool isValid, bool isWellPlaced)
        => (Char, IsValid, IsWellPlaced) = (@char, isValid, isWellPlaced);
}
