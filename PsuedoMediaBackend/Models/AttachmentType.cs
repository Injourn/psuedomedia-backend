namespace PsuedoMediaBackend.Models {
    public class AttachmentType : DbEnumeration {
        public string? MimeType { get; set; }
        public string? DisplayTag { get; set; }
    }
}
