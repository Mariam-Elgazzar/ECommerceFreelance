using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ECommerce.BL.DTO.ProductDTOs
{
    public class CreateProductDTO
    {
        [Required(ErrorMessage = "Product name is required.")]
        public string Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? AdditionalAttributes { get; set; }
        [JsonIgnore]
        public Dictionary<string, string>? AdditionalAttributesJson { get; set; }
        [Required(ErrorMessage = "Price is required.")]
        public decimal Price { get; set; }
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; }
        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Brand is required.")]
        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? AdditionalMedia { get; set; }
    }
}
