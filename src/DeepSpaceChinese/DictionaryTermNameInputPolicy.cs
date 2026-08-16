using System;

namespace DeepSpaceChinese;

internal static class DictionaryTermNameInputPolicy
{
    public const int CharacterLimit = 16;
    private const string TermNameInputObjectName = "Input Text Dummy - Term Names";
    private const string OriginalValidatorName = "TermNameInputValidator";
    private const string TranslatorNotesPath = "DictionaryNotes/Notes Content";

    public static bool IsTermNameInput(string gameObjectName, string inputValidatorName) =>
        IsTermNameInput(gameObjectName, inputValidatorName, null);

    public static bool IsTermNameInput(string gameObjectName, string inputValidatorName,
        string recipientPath)
    {
        if (IsTranslatorNotes(recipientPath))
            return false;
        return string.Equals(gameObjectName, TermNameInputObjectName, StringComparison.Ordinal) ||
               string.Equals(inputValidatorName, OriginalValidatorName, StringComparison.Ordinal);
    }

    public static bool IsTranslatorNotes(string recipientPath) =>
        (recipientPath ?? string.Empty).IndexOf(TranslatorNotesPath,
            StringComparison.Ordinal) >= 0;

    public static int ResolveCharacterLimit(bool isTranslatorNotes, int currentLimit) =>
        isTranslatorNotes ? 0 : currentLimit;

    public static char ValidateCharacter(string text, int characterIndex, char addedCharacter)
    {
        if (IsForbiddenCharacter(addedCharacter))
            return '\0';
        string upper = addedCharacter.ToString().ToUpper();
        return upper.Length == 0 ? '\0' : upper[0];
    }

    public static bool IsLegal(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > CharacterLimit)
            return false;
        foreach (char character in value)
        {
            if (IsForbiddenCharacter(character))
                return false;
        }
        return true;
    }

    public static string NormalizeForSubmit(string value) =>
        IsLegal(value) ? value.ToUpper() : string.Empty;

    private static bool IsForbiddenCharacter(char character) =>
        char.IsNumber(character) || char.IsWhiteSpace(character);
}
