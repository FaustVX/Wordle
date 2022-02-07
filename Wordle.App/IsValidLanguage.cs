using System.ComponentModel.DataAnnotations;
using Wordle.Core;

class IsValidLanguage : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => value is string lang && WordList.WordLists.ContainsKey(lang)
            ? ValidationResult.Success
            : new ValidationResult($"The language '{value}' is not found.");
}