using System.Threading.Tasks;

namespace Text2Sql.Net.Web.Services.Authentication
{
    public interface ILoginService
    {
        Task<LoginResult> SignInAsync(string username, string password);

        Task SignOutAsync();
    }
}
