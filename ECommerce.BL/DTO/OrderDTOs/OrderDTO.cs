using ECommerce.DAL.Extend;
using ECommerce.DAL.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.OrderDTOs
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public DateTime? Date{ get; set; }
        public string? Status { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public List<OrderItemDTO> OrderItems { get; set; }
    }
}
