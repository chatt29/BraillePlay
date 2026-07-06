using System.Collections.Generic;

/// <summary>
/// Converts a submitted braille chord pattern (the string broadcast by
/// BrailleMapping.OnBrailleChordSubmitted, e.g. "100000") into the letter or
/// punctuation character it represents. BrailleInputField uses this to turn
/// chords into text; numeric fields further reinterpret letters a-j as
/// digits 1-9,0, following the standard braille numeral convention (see
/// BrailleInputField.ResolveForMode).
///
/// Patterns mirror the same Grade-1 letter set BrailleMapping already uses
/// for its default letter sounds. There's no universal braille "@" chord, so
/// a placeholder pattern is assigned below - swap it for whatever chord your
/// players actually use and mention it in the signup instructions.
/// </summary>
public static class BrailleTextInput
{
    private static readonly Dictionary<string, char> LetterPatterns = new Dictionary<string, char>
    {
        { "100000", 'a' }, { "110000", 'b' }, { "100100", 'c' }, { "100110", 'd' },
        { "100010", 'e' }, { "110100", 'f' }, { "110110", 'g' }, { "110010", 'h' },
        { "010100", 'i' }, { "010110", 'j' }, { "101000", 'k' }, { "111000", 'l' },
        { "101100", 'm' }, { "101110", 'n' }, { "101010", 'o' }, { "111100", 'p' },
        { "111110", 'q' }, { "111010", 'r' }, { "011100", 's' }, { "011110", 't' },
        { "101001", 'u' }, { "111001", 'v' }, { "010111", 'w' }, { "101101", 'x' },
        { "101111", 'y' }, { "101011", 'z' },
    };

    // Extend or replace these for whatever punctuation your Username/Password fields need.
    private static readonly Dictionary<string, char> PunctuationPatterns = new Dictionary<string, char>
    {
        { "010011", '.' }, // UEB period: dots 2-5-6
        { "010000", ',' }, // UEB comma: dot 2
        { "011010", '@' }, // no standard braille chord for "@" - placeholder (dots 2-3-5), reassign as needed
    };

    /// <summary>Tries to resolve a submitted chord pattern to a character. Returns false for unmapped patterns.</summary>
    public static bool TryGetChar(string pattern, out char result)
    {
        if (LetterPatterns.TryGetValue(pattern, out result))
            return true;

        if (PunctuationPatterns.TryGetValue(pattern, out result))
            return true;

        result = '\0';
        return false;
    }
}