namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class RequestPostProtocolMessage {
        public string? PostText { get; set; }
        public PostTypeEnum PostTypeCode { get; set; }
        public string? ParentPostId { get; set; }
    }
}
