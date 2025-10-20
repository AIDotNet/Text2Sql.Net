using System.ComponentModel.DataAnnotations;

namespace Text2Sql.Net.Web.Models
{
    public class LoginParamsType
    {
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public string? Captcha { get; set; }

    public string? LoginType { get; set; }

        public bool AutoLogin { get; set; }
    }
}