using Microsoft.Extensions.Options;
using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class AccountService {
        public MongoDbService<FriendsFollowers> FriendsFollowersService { get; set; }
        public MongoDbService<RelationshipType> RelationshipTypeService { get; set; }

        public AccountService(IOptions<PsuedoMediaDatabaseSettings> psuedoMediaDatabaseSettings) {
            FriendsFollowersService = new MongoDbService<FriendsFollowers>(psuedoMediaDatabaseSettings);
            RelationshipTypeService = new MongoDbService<RelationshipType>(psuedoMediaDatabaseSettings);
        }
    }
}
