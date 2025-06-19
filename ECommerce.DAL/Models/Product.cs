using System.ComponentModel.DataAnnotations;

namespace ECommerce.DAL.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? AdditionalAttributes { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public string Brand { get; set; }
        [Required]
        public string Modal { get; set; }
        public int Quantity { get; set; }
        public string? MainImageURL { get; set; }
        public string? ImagePublicId { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductMedia> ProductMedia { get; } = new List<ProductMedia>();
    }
}
