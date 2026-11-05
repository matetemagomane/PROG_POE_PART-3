namespace RaceDay.API.Helpers
{
    /// <summary>Small helpers for reading the logged-in user from the session.</summary>
    public static class SessionHelper
    {
        public const string UserIdKey = "UserId";
        public const string UserRoleKey = "UserRole";

        public static int? GetUserId(this HttpContext ctx) => ctx.Session.GetInt32(UserIdKey);
        public static string? GetRole(this HttpContext ctx) => ctx.Session.GetString(UserRoleKey);

        public static bool IsAuthenticated(this HttpContext ctx) => ctx.GetUserId().HasValue;
        public static bool IsOrganiser(this HttpContext ctx) => ctx.IsAuthenticated() && ctx.GetRole() == "Organiser";
        public static bool IsParticipant(this HttpContext ctx) => ctx.IsAuthenticated() && ctx.GetRole() == "Participant";
    }
}
