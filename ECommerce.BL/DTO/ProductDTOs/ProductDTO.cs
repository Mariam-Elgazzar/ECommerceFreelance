using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.ProductDTOs
{
    public class ProductDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? AdditionalAttributes { get; set; } // JSON string
        public decimal Price { get; set; }
        public string? Status { get; set; }
        public string? MainImageURL { get; set; }
        public string? ImagePublicId { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductMediaDTO> ProductMedia { get; set; } = new List<ProductMediaDTO>();
    }
}
