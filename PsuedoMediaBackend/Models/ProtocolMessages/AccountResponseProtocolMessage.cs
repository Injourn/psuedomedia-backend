namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class AccountResponseProtocolMessage {
        public bool IsRelated { get; set; }
        public string? ToRelationshipType { get; set; }
        public string? FromRelationshipType { get; set; }

    }
}
