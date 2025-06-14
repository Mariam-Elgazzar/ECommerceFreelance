
using ECommerce.BL.DTO.AuthenticationDTOs;
using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;

namespace ECommerce.BL.Services.AuthenticationService
{
    public interface IAuthenticationServices
    {
        /// <summary>
        /// Registers a new user with the provided credentials and returns an authentication response.
        /// </summary>
        /// <param name="data">The registration data containing user details such as email and password.</param>
        /// <returns>An <see cref="AuthenticationDTO"/> containing authentication status, user details, JWT token, and error message if applicable.</returns>
        Task<AuthenticationDTO> Register(RegisterDTO data);
        /// <summary>
        /// Authenticates a user with the provided login credentials and returns an authentication response.
        /// </summary>
        /// <param name="data">The login data containing email and password.</param>
        /// <returns>An <see cref="AuthenticationDTO"/> containing authentication status, user details, JWT token, and error message if applicable.</returns>
        Task<AuthenticationDTO> Login(LoginDTO data);
        /// <summary>
        /// Initiates a password reset process by sending a reset link to the user's email.
        /// </summary>
        /// <param name="dto">The data containing the user's email for password reset.</param>
        /// <returns>An <see cref="ResultDTO"/> containing the status and message of the email operation.</returns>
        Task<ResultDTO> ForgetPassword(ForgetPasswordDTO dto);
        /// <summary>
        /// Resets a user's password using the provided reset token and new password.
        /// </summary>
        /// <param name="dto">The data containing email, reset token, and new password.</param>
        /// <returns>A string indicating the result of the password reset operation.</returns>
        Task<string> ResetPassword(ResetPasswordDTO dto);
        /// <summary>
        /// Changes a user's password after verifying the old password.
        /// </summary>
        /// <param name="dto">The data containing user ID, old password, and new password.</param>
        /// <returns>A string indicating the result of the password change operation.</returns>
        Task<string> ChangePassword(ChangePasswordDTO dto);
    }
}
