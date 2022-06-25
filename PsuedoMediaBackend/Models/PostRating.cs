namespace PsuedoMediaBackend.Models {
    public class PostRating : BaseEntity {
        public string? UserId { get; set; }
        public string? PostId { get; set; }
        public int Value { get; set; }
    }
}
