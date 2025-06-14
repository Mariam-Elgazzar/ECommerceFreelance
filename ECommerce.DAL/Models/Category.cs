namespace ECommerce.DAL.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageURL { get; set; }
        public string? ImageThumbnailURL { get; set; }
        public string? ImagePublicId { get; set; }
        public List<Product> Products { get; } = new List<Product>();
    }
}
