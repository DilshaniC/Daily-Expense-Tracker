// Session.cs
public static class Session
{
    public static int CurrentUserId { get; private set; }
    public static string CurrentUserName { get; private set; }

    public static void SignIn(int userId, string username)
    {
        CurrentUserId = userId;
        CurrentUserName = username;
    }

    public static void SignOut()
    {
        CurrentUserId = 0;
        CurrentUserName = null;
    }

    public static bool IsAuthenticated => CurrentUserId > 0;
}
