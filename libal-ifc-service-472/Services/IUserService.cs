using libal.Domain;
using System.Threading.Tasks;

namespace libal.Services
{
    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
    }
}