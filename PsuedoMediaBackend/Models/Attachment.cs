using MongoDB.Bson.Serialization.Attributes;
using System.Net.Mime;

namespace PsuedoMediaBackend.Models {
    public class Attachment : FadingEntity {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? AttachmentTypeId { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? PostId { get; set; }

        public string? FileName { get; set; }
    }
}
