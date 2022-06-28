using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class AccountProtocolMessage : IValidation {
        //TODO: add validation filters for all posts
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        public bool Validate(AuthenticationService authenticationService) {
            if (Id == null) {
                Users sameUsername = authenticationService.UserService.GetOneByDefinition(x => x.Username == Username).Result;
                if (sameUsername != null) {
                    throw new Exception("Username already exists, please choose a different username");
                }
            }
            else if (!string.IsNullOrEmpty(Username)) {
                return false;
            }
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) {
                throw new Exception("Required field is empty");
            }
            return true;
        }
    }
}
