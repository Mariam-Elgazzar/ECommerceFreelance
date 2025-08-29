using ECommerce.DAL.Extend;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.DAL.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public OrderStatus OrderStatus { get; set; }
        public string ProductStatus { get; set; }
        public string? RentalPeriod { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }
        [Required]
        [StringLength(255)]
        public string Address { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
    public enum OrderStatus
    {
        NewOrder,
        Processing,
        Shipped,
        Cancelled
    }
}