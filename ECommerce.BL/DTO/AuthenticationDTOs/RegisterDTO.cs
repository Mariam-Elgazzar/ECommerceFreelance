using System.ComponentModel.DataAnnotations;

namespace ECommerce.BL.DTO.AuthenticationDTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "First name is required.")] 
        [StringLength(50, ErrorMessage = "First name must not exceed 50 characters.")] 
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name must not exceed 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Address must not exceed 500 characters.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}

