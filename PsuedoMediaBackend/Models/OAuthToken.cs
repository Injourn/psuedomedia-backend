using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PsuedoMediaBackend.Models {
    public class OAuthToken : FadingEntity {
        public string? Token { get; set; }
    }
}
