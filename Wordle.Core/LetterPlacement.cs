namespace Wordle.Core;

public abstract class LetterPlacement
{
    public bool AlreadyWellPlacedLetter { get; init; }
}

public abstract class KnownLetter : LetterPlacement
{ }

public sealed class UnknownLetter : LetterPlacement
{ }

public class ValidLetter : KnownLetter
{ }

public sealed class InvalidLetter : KnownLetter
{ }

public sealed class WellPlacedLetter : ValidLetter
{ }
