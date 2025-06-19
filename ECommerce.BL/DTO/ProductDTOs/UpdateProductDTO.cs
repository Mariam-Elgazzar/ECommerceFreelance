using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.ProductDTOs
{
    public class UpdateProductDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int? Quantity { get; set; }
        public string? AdditionalAttributes { get; set; }
        [JsonIgnore]
        public Dictionary<string,string>? AdditionalAttributesJson { get; set; }
        public string? Status { get; set; }
        public int? CategoryId { get; set; }
        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? AdditionalMedia { get; set; }
        public List<string>? MediaToDelete { get; set; }
    }
}
