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

        public async Task<List<FriendsFollowers>> GetAllFriendsAndFollowing(string userId) {
            RelationshipType friendType = await RelationshipTypeService.GetByCode(RelationshipTypeEnum.FRIEND.ToString());
            RelationshipType followType = await RelationshipTypeService.GetByCode(RelationshipTypeEnum.FOLLOW.ToString());

            return await FriendsFollowersService.GetAllByDefinition(x => x.UserAId == userId && (x.RelationShipTypeId == friendType.Id || x.RelationShipTypeId == followType.Id));
        }
    }
}
