using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class RequestPostProtocolMessage : IValidation {
        public string? PostText { get; set; }
        public PostTypeEnum PostTypeCode { get; set; }
        public string? ParentPostId { get; set; }

        public bool Validate(AuthenticationService authenticationService) {
            if (string.IsNullOrEmpty(PostText)) {
                throw new Exception("PostMessage is empty");
            }
            else if(PostText?.Length > 256) {
                throw new Exception("Post is too long");
            }
            return true;
        }
    }
}
