namespace Text2Sql.Net.Web.Services.Authentication
{
    public sealed class LoginResult
    {
        private LoginResult(bool succeeded, string? errorMessage, string? displayName)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
            DisplayName = displayName;
        }

        public bool Succeeded { get; }

        public string? ErrorMessage { get; }

        public string? DisplayName { get; }

        public static LoginResult Success(string? displayName) => new(true, null, displayName);

        public static LoginResult Fail(string errorMessage) => new(false, errorMessage, null);
    }
}
