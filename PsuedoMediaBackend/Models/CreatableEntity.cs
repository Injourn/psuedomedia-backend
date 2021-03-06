using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PsuedoMediaBackend.Models {
    public abstract class CreatableEntity : BaseEntity {
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CreatedByUserId { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
