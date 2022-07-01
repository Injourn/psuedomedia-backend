namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class PostProtocolResponseMessage {
        public List<PostProtocolObject> Statuses { get; set; }
        public long Count { get; set; }
    }
}
