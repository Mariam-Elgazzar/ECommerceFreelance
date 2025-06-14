
using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;

namespace ECommerce.BL.Services.EmailServices
{
    public interface IEmailServices
    {
        /// <summary>
        /// Asynchronously sends an email using the provided email data.
        /// </summary>
        /// <param name="dto">The email data containing recipient, subject, and content.</param>
        /// <returns>An <see cref="ResultDTO"/> indicating the success or failure of the email operation.</returns>
        Task<ResultDTO> SendEmailAsync(EmailDTO dto);
    }
}
