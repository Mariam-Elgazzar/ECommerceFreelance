using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.ProductDTOs
{
    public class ProductMediaDTO
    {
        public int Id { get; set; }
        public string MediaURL { get; set; }
        public string? ImageThumbnailURL { get; set; }
        public string? MediaPublicId { get; set; }
    }
}
