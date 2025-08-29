using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.UserDTOs;
using ECommerce.BL.Specification.UserSpecification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.Services.UserServices
{
    public interface IUserServices
    {
        /// <summary>
        /// Retrieves a paginated list of users based on optional parameters.
        /// </summary>
        /// <param name="param">Optional parameters for filtering and pagination (defaults to new UserParams if null).</param>
        /// <returns>A PaginationResponse containing the list of UserDTOs, page size, page index, and total count.</returns>
        Task<PaginationResponse<UserDTO>> GetAllUsersAsync(UserParams param = null);
        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>A UserDTO containing user details if found; otherwise, null.</returns>
        Task<UserDTO> GetUserByIdAsync(string userId);

        //Task<List<UserDTO>> GetAllUsersAsync(bool? isDeleted = null);

        /// <summary>
        /// Updates a user's details based on the provided DTO.
        /// </summary>
        /// <param name="userDto">The data transfer object containing updated user information.</param>
        /// <returns>A string indicating success or failure with a relevant message.</returns>
        Task<string> UpdateUserAsync(UserDTO user);
        /// <summary>
        /// Toggles a user's block status based on the provided ID and block flag.
        /// </summary>
        /// <param name="id">The ID of the user to block or unblock.</param>
        /// <param name="isBlock">True to block the user, false to unblock.</param>
        /// <returns>A string indicating success or failure with a relevant message.</returns>
        Task<string> ToggleUserBlockAsync(string id, bool isBlock);
    }
}
