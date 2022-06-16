using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class UsersService : MongoDbService<Users> {

        public UsersService() : base() {            
        }


    }
}
