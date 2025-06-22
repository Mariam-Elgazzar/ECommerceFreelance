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
        [Required(ErrorMessage = "Brand is required.")]
        public string Brand { get; set; }
        [Required(ErrorMessage = "Model is required.")]
        public string Model { get; set; }
        public string? AdditionalAttributes { get; set; }
        [JsonIgnore]
        public Dictionary<string, string>? AdditionalAttributesJson { get; set; }
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; }
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative integer.")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "MainImage is required.")]
        public IFormFile MainImage { get; set; }
        public List<IFormFile>? AdditionalMedia { get; set; }
    }
}
