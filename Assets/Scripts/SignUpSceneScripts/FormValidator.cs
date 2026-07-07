/// <summary>
/// Stateless, reusable field-validation rules shared by every accessible
/// form in the project. Each method returns null when the value is valid,
/// or a spoken error message describing what's wrong.
///
/// Firestore-based checks (e.g. "is this student number already taken")
/// are asynchronous and don't belong here - those live in the Firestore
/// service classes and are run separately by the sign-up managers.
/// </summary>
public static class FormValidator
{
    public static string ValidateRequired(string label, string value)
    {
        return string.IsNullOrWhiteSpace(value) ? label + " cannot be empty." : null;
    }

    public static string ValidateLettersOnly(string label, string value, int minLength = 1)
    {
        string requiredError = ValidateRequired(label, value);
        if (requiredError != null) return requiredError;

        if (value.Length < minLength)
            return label + " must be at least " + minLength + " characters.";

        foreach (char c in value)
        {
            if (!char.IsLetter(c))
                return label + " can only contain letters.";
        }

        return null;
    }

    public static string ValidateDigitsOnly(string label, string value, int minLength = 1)
    {
        string requiredError = ValidateRequired(label, value);
        if (requiredError != null) return requiredError;

        if (value.Length < minLength)
            return label + " must be at least " + minLength + " digits.";

        foreach (char c in value)
        {
            if (!char.IsDigit(c))
                return label + " can only contain numbers.";
        }

        return null;
    }

    public static string ValidateMinLength(string label, string value, int minLength)
    {
        string requiredError = ValidateRequired(label, value);
        if (requiredError != null) return requiredError;

        return value.Length < minLength ? label + " must be at least " + minLength + " characters." : null;
    }
}