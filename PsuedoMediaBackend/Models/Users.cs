namespace PsuedoMediaBackend.Models {
    public class Users : CreatableEntity {
        public string? DisplayName { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
