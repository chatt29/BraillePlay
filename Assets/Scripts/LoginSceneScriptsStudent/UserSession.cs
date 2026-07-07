using UnityEngine;

/// <summary>
/// Stores the currently logged-in user for the duration of the app.
/// This is a static class, so it automatically survives scene changes.
/// </summary>
public static class UserSession
{
    // Student
    public static string StudentNumber { get; private set; }

    // Teacher
    public static string TeacherUsername { get; private set; }

    // Current role
    public static bool IsStudentLoggedIn => !string.IsNullOrEmpty(StudentNumber);
    public static bool IsTeacherLoggedIn => !string.IsNullOrEmpty(TeacherUsername);

    /// <summary>
    /// Called after a successful student login.
    /// </summary>
    public static void SetStudent(string studentNumber)
    {
        StudentNumber = studentNumber;
        TeacherUsername = null;
    }

    /// <summary>
    /// Called after a successful teacher login.
    /// </summary>
    public static void SetTeacher(string username)
    {
        TeacherUsername = username;
        StudentNumber = null;
    }

    /// <summary>
    /// Clears the current session (used when logging out).
    /// </summary>
    public static void Clear()
    {
        StudentNumber = null;
        TeacherUsername = null;
    }
}