using ECommerce.BL.DTO.UserDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Specification.UserSpecification;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUnitOfWork unitOfWork, ILogger<UsersController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Get All Users

        /// <summary>
        /// Retrieves a list of all registered users with filtering, sorting, and pagination.
        /// </summary>
        /// <param name="param">Parameters for filtering (search, isDeleted), sorting, and pagination.</param>
        /// <returns>
        /// Returns a paginated list of user details if successful, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves all users from the system. Only users with Admin role can access this endpoint.
        /// The response includes user details such as ID, first name, last name, email, phone number, and address,
        /// along with pagination metadata (page size, page index, total count).
        ///
        /// Query Parameters:
        /// - search: Filters users by email, first name, or last name (e.g., "john").
        /// - isDeleted: Filters users by deletion status (true/false, omit for all).
        /// - sortProp: Sorts by Id, FName, or LName (default: Id).
        /// - sortDirection: Sorts in Ascending or Descending order (default: Ascending).
        /// - pageIndex: The page number to retrieve (default: 1).
        /// - pageSize: Number of users per page (default: 10, max: 10).
        ///
        /// Example Request:
        /// GET /api/User/GetAllUsers?search=john&isDeleted=false&sortProp=FName&sortDirection=Ascending&pageIndex=2&pageSize=5
        ///
        /// Example Response:
        /// ```json
        /// {
        ///   "pageSize": 5,
        ///   "pageIndex": 2,
        ///   "totalCount": 12,
        ///   "data": [
        ///     {
        ///       "id": "12345",
        ///       "firstName": "John",
        ///       "lastName": "Doe",
        ///       "email": "john.doe@example.com",
        ///       "phoneNumber": "123-456-7890",
        ///       "address": "123 Main St"
        ///     },
        ///     {
        ///       "id": "67890",
        ///       "firstName": "Johnny",
        ///       "lastName": "Smith",
        ///       "email": "johnny.smith@example.com",
        ///       "phoneNumber": "987-654-3210",
        ///       "address": "456 Oak Ave"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a paginated list of user details when the operation is successful.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "pageSize": 10,
        ///   "pageIndex": 1,
        ///   "totalCount": 20,
        ///   "data": [
        ///     {
        ///       "id": "12345",
        ///       "firstName": "John",
        ///       "lastName": "Doe",
        ///       "email": "john.doe@example.com",
        ///       "phoneNumber": "123-456-7890",
        ///       "address": "123 Main St"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </response>
        /// <response code="204">
        /// Returned when no users are found matching the criteria.
        /// No Content Response (204):
        /// ```json
        /// {}
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized to access this endpoint.
        /// Unauthorized Response (401):
        /// ```json
        /// {
        ///   "message": "Unauthorized access"
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
        [HttpGet]
        [Route("~/Users/GetAllUsers")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserParams param = null)
        {
            try
            {
                _logger.LogInformation("Retrieving users with parameters: Search={Search}, IsDeleted={IsDeleted}, SortProp={SortProp}, SortDirection={SortDirection}, PageIndex={PageIndex}, PageSize={PageSize}",
                    param?.Search, param?.IsDeleted, param?.SortProp, param?.SortDirection, param?.PageIndex, param?.PageSize);

                var response = await _unitOfWork.UserServices.GetAllUsersAsync(param);

                if (response == null || !response.Data.Any())
                {
                    _logger.LogWarning("No users found for parameters: Search={Search}, IsDeleted={IsDeleted}", param?.Search, param?.IsDeleted);
                    return StatusCode(StatusCodes.Status204NoContent, new { Message = "No users found." });
                }

                _logger.LogInformation("Retrieved {UserCount} users for parameters: Search={Search}, IsDeleted={IsDeleted}", response.Data.Count, param?.Search, param?.IsDeleted);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for parameters: Search={Search}, IsDeleted={IsDeleted}", param?.Search, param?.IsDeleted);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing your request" });
            }
        }

        #endregion


        #region Get User By Id

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user to retrieve.</param>
        /// <returns>
        /// Returns the user details if found, or an error message if the user does not exist or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves a single user from the system by their ID. Only users with Admin role can access this endpoint.
        /// The response includes user details such as ID, first name, last name, email, phone number, and address.
        ///
        /// Validation rules for request:
        /// - Id: Required, must be a valid user identifier, e.g., "12345".
        ///
        /// Example Request:
        /// ```
        /// GET /Users/GetUserById/12345
        /// ```
        ///
        /// Example Response:
        /// ```json
        /// {
        ///   "id": "12345",
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the user details when the user is found.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "id": "12345",
        ///   "firstName": "John",
        ///     "lastName": "Doe",
        ///     "email": "user@example.com",
        ///     "phoneNumber": "123-456-7890",
        ///     "address": "123 Main St"
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the provided user ID is invalid.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Invalid user ID."
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when no user is found with the provided ID.
        /// Not Found Response (404):
        /// ```json
        /// {
        ///   "message": "User not found."
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized to access this endpoint.
        /// Unauthorized Response (401):
        /// ```json
        /// {
        ///   "message": "Unauthorized access"
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
        [HttpGet]
        [Route("~/Users/GetUserById/{id}")]
        //[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                // Validate the ID
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Invalid user ID provided in GetUserById request.");
                    return BadRequest(new { Message = "Invalid user ID." });
                }

                // Call the service to retrieve the user
                var user = await _unitOfWork.UserServices.GetUserByIdAsync(id);

                // Check if the user was found
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return NotFound(new { Message = "User not found." });
                }

                // Log successful retrieval
                _logger.LogInformation("Successfully retrieved user with ID: {UserId}.", id);

                // Return successful response
                return Ok(user);
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError(ex, "Unexpected error while retrieving user with ID: {UserId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while processing your request"
                });
            }
        }

        #endregion


        #region Update User

        /// <summary>
        /// Updates a user's details using their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user to update.</param>
        /// <param name="userDto">The data transfer object containing updated user details.</param>
        /// <returns>
        /// Returns a success message if the update is successful, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint updates a user's details in the system. Only users with Admin role can access this endpoint.
        /// The request must include a valid user ID in the URL and a valid user DTO in the request body.
        ///
        /// Validation rules for request body:
        /// - Id: Must match the ID in the URL, required, e.g., "12345".
        /// - FirstName: Required, maximum length of 50 characters, e.g., "John".
        /// - LastName: Required, maximum length of 50 characters, e.g., "Doe".
        /// - Email: Required, must be a valid email format, e.g., "user@example.com".
        /// - PhoneNumber: Optional, maximum length of 20 characters, must be a valid phone number format if provided, e.g., "123-456-7890".
        /// - Address: Optional, maximum length of 500 characters, e.g., "123 Main St".
        ///
        /// Example Request:
        /// ```
        /// PUT /Users/UpdateUser/12345
        /// ```
        /// ```json
        /// {
        ///   "id": "12345",
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St"
        /// }
        /// ```
        ///
        /// Example Response:
        /// ```json
        /// {
        ///   "message": "User updated successfully"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the user is updated successfully.
        /// </response>
        /// <response code="400">
        /// Returned when the user ID is invalid, the request body is invalid, or the ID in the URL does not match the DTO.
        /// </response>
        /// <response code="404">
        /// Returned when no user is found with the provided ID.
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized to access this endpoint.
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// </response>
        [HttpPut]
        [Route("~/Users/UpdateUser/{id}")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserDTO userDto)
        {
            try
            {
                // Validate the ID and DTO
                if (string.IsNullOrWhiteSpace(id) || userDto == null)
                {
                    _logger.LogWarning("Invalid user ID or request body in UpdateUser request.");
                    return BadRequest(new { Message = "Invalid user ID or request data." });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for UpdateUser request with user ID: {UserId}", id);
                    return BadRequest(ModelState);
                }

                // Ensure ID in URL matches ID in DTO
                if (id != userDto.Id)
                {
                    _logger.LogWarning("User ID in URL {UrlId} does not match ID in DTO {DtoId}.", id, userDto.Id);
                    return BadRequest(new { Message = "User ID in URL does not match ID in request body." });
                }

                // Call the service to update the user
                var result = await _unitOfWork.UserServices.UpdateUserAsync(userDto);

                // Check if the update failed
                if (result.Contains("User not found") || result.Contains("Failed to update user") || result.Contains("An error occurred"))
                {
                    _logger.LogWarning("Update failed for user ID: {UserId}. Reason: {Message}", id, result);
                    return NotFound(new { Message = result });
                }

                // Log successful update
                _logger.LogInformation("Successfully updated user with ID: {UserId}.", id);

                // Return successful response
                return Ok(new { Message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError(ex, "Unexpected error while updating user with ID: {UserId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while processing your request"
                });
            }
        }

        #endregion


        #region Toggle User Block Status

        /// <summary>
        /// Toggles a user's block status by setting their soft-deleted flag.
        /// </summary>
        /// <param name="id">The unique identifier of the user to block or unblock.</param>
        /// <param name="isBlock">Boolean indicating whether to block (true) or unblock (false) the user.</param>
        /// <returns>
        /// Returns a success message if the user's block status is toggled successfully, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint toggles a user's block status in the system by setting their IsDeleted flag to true (block) or false (unblock).
        /// Only users with Admin role can access this endpoint. The user must exist, and the operation must be valid (e.g., cannot block an already blocked user).
        ///
        /// Validation rules for request:
        /// - Id: Required, must be a valid user identifier, e.g., "12345".
        /// - isBlock: Required, must be true to block or false to unblock.
        ///
        /// Example Requests:
        /// ```
        /// POST /Users/ToggleUserBlock/12345/true
        /// ```
        /// ```
        /// POST /Users/ToggleUserBlock/12345/false
        /// ```
        ///
        /// Example Response (Block):
        /// ```json
        /// {
        ///   "message": "User blocked successfully"
        /// }
        /// ```
        /// Example Response (Unblock):
        /// ```json
        /// {
        ///   "message": "User unblocked successfully"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the user's block status is toggled successfully.
        /// </response>
        /// <response code="400">
        /// Returned when the provided user ID is invalid.
        /// </response>
        /// <response code="404">
        /// Returned when no user is found with the provided ID or the user is already in the requested block state.
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized to access this endpoint.
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// </response>
        [HttpPost]
        [Route("~/Users/ToggleUserBlock/{id}/{isBlock}")]
        //[Authorize(Roles = $"{Roles.Admin}")]
        public async Task<IActionResult> ToggleUserBlock(string id, bool isBlock)
        {
            try
            {
                // Validate the ID
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Invalid user ID provided in ToggleUserBlock request.");
                    return BadRequest(new { Message = "Invalid user ID." });
                }

                // Call the service to toggle the user's block status
                var result = await _unitOfWork.UserServices.ToggleUserBlockAsync(id, isBlock);

                // Check if the operation failed
                if (result.Contains("User not found") || result.Contains("already blocked") || result.Contains("not blocked") || result.Contains("Failed to"))
                {
                    _logger.LogWarning("Toggle block status failed for user ID: {UserId}. Reason: {Message}", id, result);
                    return NotFound(new { Message = result });
                }

                // Log successful operation
                _logger.LogInformation("Successfully {Action} user with ID: {UserId}.", isBlock ? "blocked" : "unblocked", id);

                // Return successful response
                return Ok(new { Message = $"User {(isBlock ? "blocked" : "unblocked")} successfully" });
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError(ex, "Unexpected error while toggling block status for user with ID: {UserId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while processing your request"
                });
            }
        }

        #endregion
    }
}