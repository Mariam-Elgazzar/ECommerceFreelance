using System.Text.Json.Serialization;

namespace ECommerce.BL.DTO.AuthenticationDTOs
{
    public class AuthenticationDTO
    {
        public string Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        [JsonIgnore]
        public bool IsAuthenticated { get; set; }
        public string Roles { get; set; }
    }
}
