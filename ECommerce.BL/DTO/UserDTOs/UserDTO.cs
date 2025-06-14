using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.UserDTOs
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Email { get; set; }
        public bool IsDeleted { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

    }
}
