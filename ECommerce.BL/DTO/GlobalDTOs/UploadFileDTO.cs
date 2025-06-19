using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.GlobalDTOs
{
    public class UploadFileDTO
    {
        public bool IsSuccess { get; set; }
        public string MediaUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; } = string.Empty;
        public string? PublicId { get; set; }
    }
}
