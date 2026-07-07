using Firebase.Firestore;

/// <summary>
/// Maps to a document under Teachers/{username} in Firestore.
///
/// Field names are pinned explicitly (lowerCamelCase) for the same reason
/// as StudentData - so this always matches whatever's actually in the
/// console, regardless of the C# property names on this side.
///
/// SECURITY NOTE: this stores the password as plain text, matching the
/// field the project README lists under Teachers. That's not safe for a
/// real deployment - anyone with Firestore read access (including via a
/// misconfigured security rule) can read every teacher's password in plain
/// text. Before shipping, prefer Firebase Authentication (email/password or
/// custom auth) for the credential itself, and keep this document only for
/// profile fields like FirstName/LastName. If you must keep a password
/// field here, hash it (e.g. with a salted algorithm) rather than storing
/// it verbatim - flagging this so it isn't missed rather than changing your
/// schema for you.
/// </summary>
[FirestoreData]
public class TeacherData
{
    [FirestoreProperty("firstName")]
    public string FirstName { get; set; }

    [FirestoreProperty("lastName")]
    public string LastName { get; set; }

    [FirestoreProperty("username")]
    public string Username { get; set; }

    [FirestoreProperty("password")]
    public string Password { get; set; }
}