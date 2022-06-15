using MongoDB.Bson.Serialization.Attributes;

namespace PsuedoMediaBackend.Models {
    public class FriendsFollowers : BaseEntity {

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? UserAId { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? UserBId { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? RelationShipTypeId { get; set; }
    }
}
