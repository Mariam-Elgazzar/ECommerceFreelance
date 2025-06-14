using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.AuthenticationDTOs
{
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Old password is required.")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public string UserId { get; set; }
    }
}
