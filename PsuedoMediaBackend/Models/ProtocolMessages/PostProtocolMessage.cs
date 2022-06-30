namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class PostProtocolMessage {
        public List<PostProtocolMessage> Replies { get; set; }
        public string? Message { get; set; }
        public string? UserCreatedName { get; set; }
        public string? UserCreatedById { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Id { get; set; }
        public long Rating { get; set; }
        public int UserRating { get; set; }
        public string? AttachmentId { get; set; }
        public string? AttachmentTag { get; set; }

        public PostProtocolMessage() {
            Replies = new List<PostProtocolMessage>();
        }
    }
}
