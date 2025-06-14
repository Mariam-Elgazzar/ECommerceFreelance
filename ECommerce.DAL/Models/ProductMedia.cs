using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.DAL.Models
{
    [PrimaryKey(nameof(ProductId), nameof(MediaURL))]
    [Table("ProductsMedia")]
    public class ProductMedia
    {
        public string MediaURL { get; set; }
        public string? ImageThumbnailURL { get; set; }
        public string? MediaPublicId { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
