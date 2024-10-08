using Stem.Data.Base;
using Stem.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.Repository
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository() : base() { }
        public UserRepository(STEMKITshopDBContext context) : base(context) { }

        public User GetUserByUsernameAndPassword(string username, string password)
        {
            return _context.Set<User>()
                .FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}
