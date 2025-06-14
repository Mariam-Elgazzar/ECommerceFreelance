using System.ComponentModel.DataAnnotations;

namespace ECommerce.BL.DTO.AuthenticationDTOs
{
    public class ForgetPasswordDTO
    {
        [Required(ErrorMessage = "This Field Required")]
        [EmailAddress(ErrorMessage = "Invalid Mail")]
        public string Email { get; set; }
    }
}
