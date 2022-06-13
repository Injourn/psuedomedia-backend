using MongoDB.Bson.Serialization.Attributes;

namespace PsuedoMediaBackend.Models {
    public class FriendsFollowers : BaseEntity {

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? UserAId { get; set; }

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? UserBId { get; set; }

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? RelationShipTypeId { get; set; }
    }
}
