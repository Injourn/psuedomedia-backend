namespace PsuedoMediaBackend.Models {
    public abstract class DbEnumeration : BaseEntity {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get;set; }
    }
}
