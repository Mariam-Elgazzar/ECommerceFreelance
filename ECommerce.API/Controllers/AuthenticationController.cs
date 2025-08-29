using ECommerce.BL.DTO.AuthenticationDTOs;
using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }


        #region Register 

        /// <summary>
        /// Registers a new user and returns authentication details including a token if successful.
        /// </summary>
        /// <param name="data">The registration data transfer object containing user details.</param>
        /// <returns>
        /// Returns an authentication result with a token if successful, or an error message if registration fails.
        /// </returns>
        /// <remarks>
        /// This endpoint creates a new user account with the provided details and returns an authentication token if successful.
        /// The request body must contain valid user information including first name, last name, email, password, and optional fields.
        /// 
        /// Validation rules for request body:
        /// - FirstName: Required, maximum length of 50 characters, e.g., "John".
        /// - LastName: Required, maximum length of 50 characters, e.g., "Doe".
        /// - Email: Required, must be a valid email format, e.g., "user@example.com".
        /// - PhoneNumber: Optional, maximum length of 20 characters, must be a valid phone number format if provided, e.g., "123-456-7890".
        /// - Address: Optional, maximum length of 500 characters, e.g., "123 Main St".
        /// - Password: Required, must meet system password requirements (e.g., minimum length, special characters), e.g., "P@ssw0rd123".
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St",
        ///   "password": "P@ssw0rd123"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the authentication details including token when registration is successful.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "message": "Registration successful",
        ///   "id": "12345",
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St",
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "roles": ["User"]
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the registration data is invalid, the user already exists, or the request is malformed.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Validation failed: First name is required; Invalid email format",
        ///   "id": null,
        ///   "firstName": null,
        ///   "lastName": null,
        ///   "email": null,
        ///   "phoneNumber": null,
        ///   "address": null,
        ///   "token": null,
        ///   "roles": null
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request",
        ///   "id": null,
        ///   "firstName": null,
        ///   "lastName": null,
        ///   "email": null,
        ///   "phoneNumber": null,
        ///   "address": null,
        ///   "token": null,
        ///   "roles": null
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Authentication/Register")]
        public async Task<ActionResult<AuthenticationDTO>> Register([FromBody] RegisterDTO data)
        {
            try
            {
                var result = await _unitOfWork.AuthenticationServices.Register(data);

                if (!result.IsAuthenticated)
                {
                    _logger.LogWarning("Registration failed for email: {Email}. Reason: {Message}",
                        data.Email, result.Message);
                    var errorMessage = result.Message.Split(';');
                    return BadRequest(errorMessage);
                }

                _logger.LogInformation("Successfully registered user: {Email}", data.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email: {Email}", data.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message
                });
            }
        }
        #endregion


        #region Login

        /// <summary>
        /// Authenticates a user and returns authentication details including a token if successful.
        /// </summary>
        /// <param name="data">The login data transfer object containing user credentials.</param>
        /// <returns>
        /// Returns an authentication result with a token if login is successful, or an error message if authentication fails.
        /// </returns>
        /// <remarks>
        /// This endpoint authenticates a user with the provided email and password. If the credentials are valid, it returns an authentication token and user details.
        /// The request body must contain a valid email and password.
        /// 
        /// Validation rules for request body:
        /// - Email: Required, must be a valid email format, e.g., "user@example.com".
        /// - Password: Required, must match the user's stored password, e.g., "P@ssw0rd123".
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "email": "user@example.com",
        ///   "password": "P@ssw0rd123"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the authentication details including token when login is successful.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "message": "Login successful",
        ///   "id": "12345",
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St",
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "roles": ["User"]
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the login credentials are invalid or the request is malformed.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Invalid email or password",
        ///   "id": null,
        ///   "firstName": null,
        ///   "lastName": null,
        ///   "email": null,
        ///   "phoneNumber": null,
        ///   "address": null,
        ///   "token": null,
        ///   "roles": null
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request",
        ///   "id": null,
        ///   "firstName": null,
        ///   "lastName": null,
        ///   "email": null,
        ///   "phoneNumber": null,
        ///   "address": null,
        ///   "token": null,
        ///   "roles": null
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Authentication/Login")]
        public async Task<ActionResult<AuthenticationDTO>> Login([FromBody] LoginDTO data)
        {
            try
            {
                var result = await _unitOfWork.AuthenticationServices.Login(data);

                if (!result.IsAuthenticated)
                {
                    _logger.LogWarning("Login failed for email: {Email}. Reason: {Message}",
                        data.Email, result.Message);
                    var errorMessage = result.Message.Split(';');
                    return BadRequest(errorMessage);
                }

                _logger.LogInformation("Successfully logged in user: {Email}", data.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for email: {Email}", data.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message
                });
            }
        }

        #endregion


        #region Forget Password

        /// <summary>
        /// Initiates the password reset process by sending a reset email to the user.
        /// </summary>
        /// <param name="dto">The data transfer object containing the user's email.</param>
        /// <returns>
        /// Returns a response indicating whether the password reset email was sent successfully or an error message if the process fails.
        /// </returns>
        /// <remarks>
        /// This endpoint initiates a password reset by sending an email with a reset link to the provided email address. 
        /// If the email is not registered, an error is returned.
        ///
        /// Validation rules for request body:
        /// - Email: Required, must be a valid email format, e.g., "user@example.com".
        ///
        /// Example Request:
        /// ```json
        /// {
        ///   "email": "user@example.com"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the password reset email is sent successfully.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "message": "Password reset email sent successfully"
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the email is not registered or the request is malformed.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Email is not registered!"
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Authentication/ForgetPassword")]
        public async Task<ActionResult<ResultDTO>> ForgetPassword([FromBody] ForgetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for ForgetPassword request with email: {Email}", dto?.Email);
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _unitOfWork.AuthenticationServices.ForgetPassword(dto);

                if (result.Message.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    result.Message.Contains("not registered", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Password reset failed for email: {Email}. Reason: {Message}",
                        dto.Email, result.Message);
                    return BadRequest(new { result.Message });
                }

                _logger.LogInformation("Password reset email sent successfully for email: {Email}", dto.Email);

                return Ok(new { Message = "Password reset email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset for email: {Email}", dto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message
                });
            }
        }

        #endregion


        #region Reset Password

        /// <summary>
        /// Resets a user's password using a provided email, reset token, and new password.
        /// </summary>
        /// <param name="dto">The data transfer object containing the email, reset token, and new password.</param>
        /// <returns>
        /// Returns a response indicating whether the password reset was successful or an error message if the process fails.
        /// </returns>
        /// <remarks>
        /// This endpoint resets a user's password using a token sent via email during the forget password process.
        /// The request must include a valid email, a reset token, and a new password.
        ///
        /// Validation rules for request body:
        /// - Email: Required, must be a valid email format, e.g., "user@example.com".
        /// - Token: Required, must be a valid password reset token.
        /// - Password: Required, must meet password policy requirements (e.g., minimum length, special characters).
        ///
        /// Example Request:
        /// ```json
        /// {
        ///   "email": "user@example.com",
        ///   "token": "CfDJ8...encodedToken...",
        ///   "password": "NewP@ssw0rd123"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the password is reset successfully.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "message": "Password reset successful"
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the email is not registered, the token is invalid, or the request is malformed.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Email is not registered!"
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Authentication/ResetPassword")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for ResetPassword request with email: {Email}", dto?.Email);
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _unitOfWork.AuthenticationServices.ResetPassword(dto);

                if (result.Contains("Email is not registered!") || result.Contains("Failed to reset password") || result.Contains("An error occurred"))
                {
                    _logger.LogWarning("Password reset failed for email: {Email}. Reason: {Message}", dto.Email, result);
                    return BadRequest(new { Message = result });
                }

                _logger.LogInformation("Password reset successful for email: {Email}", dto.Email);

                return Ok(new { Message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset for email: {Email}", dto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message
                });
            }
        }


        #endregion


        #region Change Password

        /// <summary>
        /// Changes a user's password using their user ID, old password, and new password.
        /// </summary>
        /// <param name="dto">The data transfer object containing the user ID, old password, and new password.</param>
        /// <returns>
        /// Returns a response indicating whether the password change was successful or an error message if the process fails.
        /// </returns>
        /// <remarks>
        /// This endpoint allows an authenticated user to change their password by providing their user ID, current (old) password, and a new password.
        /// The old password must be correct, and the new password must meet policy requirements and differ from the old password.
        ///
        /// Validation rules for request body:
        /// - UserId: Required, must be a valid user identifier.
        /// - OldPassword: Required, must match the user's current password.
        /// - NewPassword: Required, must meet password policy requirements (e.g., minimum length, special characters) and cannot be the same as the old password.
        ///
        /// Example Request:
        /// ```json
        /// {
        ///   "userId": "12345",
        ///   "oldPassword": "OldP@ssw0rd123",
        ///   "newPassword": "NewP@ssw0rd123"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the password is changed successfully.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "message": "Password changed successfully"
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the user ID is invalid, old password is incorrect, new password is invalid, or the request is malformed.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Old password is incorrect."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Authentication/ChangePassword")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for ChangePassword request with user ID: {UserId}", dto?.UserId);
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _unitOfWork.AuthenticationServices.ChangePassword(dto);

                if (result.Contains("User not found!") ||
                    result.Contains("Old password is incorrect.") ||
                    result.Contains("New password cannot be the same") ||
                    result.Contains("Failed to change password") ||
                    result.Contains("An error occurred"))
                {
                    _logger.LogWarning("Password change failed for user ID: {UserId}. Reason: {Message}", dto.UserId, result);
                    return BadRequest(new { Message = result });
                }

                _logger.LogInformation("Password changed successfully for user ID: {UserId}", dto.UserId);

                return Ok(new { Message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password change for user ID: {UserId}", dto.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message
                });
            }
        }

        #endregion
    }
}


