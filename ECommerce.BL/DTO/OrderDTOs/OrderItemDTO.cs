using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.OrderDTOs
{
    public class OrderItemDTO
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string ProductStatus { get; set; }
        public string? RentalPeriod { get; set; }
    }
}
