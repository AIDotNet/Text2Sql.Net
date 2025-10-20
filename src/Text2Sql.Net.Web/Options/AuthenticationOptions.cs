using System.Collections.Generic;

namespace Text2Sql.Net.Web.Options
{
    public sealed class AuthenticationOptions
    {
        public IList<UserCredential> Users { get; set; } = new List<UserCredential>();
    }

    public sealed class UserCredential
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
    }
}
