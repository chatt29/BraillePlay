/// <summary>
/// Remembers that the learner is coming back from a finished lesson,
/// so the menu opens on the lesson row instead of the guides.
/// Static: survives scene loads, resets when the app closes.
/// </summary>
public static class MenuReturn
{
    public static bool CameFromLesson;
    public static int LessonNumber;

    /// <summary>Returns true once (then clears the flag) and gives the lesson that was just finished.</summary>
    public static bool Consume(out int lessonNumber)
    {
        bool value = CameFromLesson;
        lessonNumber = LessonNumber;
        CameFromLesson = false;
        return value;
    }
}