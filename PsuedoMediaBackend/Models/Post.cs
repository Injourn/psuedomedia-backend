using MongoDB.Bson.Serialization.Attributes;

namespace PsuedoMediaBackend.Models {
    public class Post : FadingEntity {
        public string? PostText { get; set; }
        public string PostTypeId { get; set; }

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? ParentPostId { get; set; }
    }
}
