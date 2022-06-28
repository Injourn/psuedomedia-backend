using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public interface IValidation {
        public bool Validate(AuthenticationService authenticationService);
    }
}
