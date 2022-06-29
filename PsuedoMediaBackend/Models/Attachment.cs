using MongoDB.Bson.Serialization.Attributes;
using System.Net.Mime;

namespace PsuedoMediaBackend.Models {
    public class Attachment : FadingEntity {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? AttachmentTypeId { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? PostId { get; set; }

        public string? FileName { get; set; }
        public string? FileSystemFileName { get; set; }
    }
}
