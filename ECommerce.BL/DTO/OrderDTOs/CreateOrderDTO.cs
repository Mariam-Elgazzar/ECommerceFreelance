using System.ComponentModel.DataAnnotations;

namespace ECommerce.BL.DTO.OrderDTOs
{
    public class CreateOrderDTO
    {
        [Required(ErrorMessage = "Name is quantity")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }
        public string? RentalPeriod { get; set; }
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }
    }
}
