using libal.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace libal.Services
{
    public class UserService : IUserService
    {
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private List<User> _users = new List<User>
        {
            new User {Username = "libal-ifc-service", Password = "9}gREH5wr$Ekk`>6cAdkGc~wx[B!,x7.{yRX/_~f/xBBXuUw&C)'Sue}w+H/y&U@" }
        };

        public async Task<User> Authenticate(string username, string password)
        {
            var user = await Task.Run(() => _users.SingleOrDefault(x => x.Username == username && x.Password == password));

            // return null if user not found
            if (user == null)
                return null;

            // authentication successful so return user details without password
            user.Password = null;
            return user;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            // return users without passwords
            return await Task.Run(() => _users.Select(x => {
                x.Password = null;
                return x;
            }));
        }
    }
}
