namespace ECommerce.BL.DTO.OrderDTOs
{
    public class CheckoutDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; } 
        public string? RentalPeriod { get; set; }
        public string Status { get; set; }
        public string ProductName { get; set; }
        public string ProductCategory { get; set; }
        public string Modal { get; set; }
        public string Brand { get; set; }
    }
}