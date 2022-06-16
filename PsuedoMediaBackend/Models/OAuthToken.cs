using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PsuedoMediaBackend.Models {
    public class OAuthToken : FadingEntity {
        public string? Token { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }
        public bool IsInactive { get; set; }
    }
}
