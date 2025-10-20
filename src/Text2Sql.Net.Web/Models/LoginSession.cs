using System;

namespace Text2Sql.Net.Web.Models
{
    public sealed class LoginSession
    {
        public string Username { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public DateTimeOffset SignedInAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Username);
    }
}
