using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.UserDTOs;
using ECommerce.BL.Specification.UserSpecification;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Data;
using ECommerce.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.Services.UserServices
{
    public class UserServices : IUserServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserServices(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        #region Get All Users Async
        /// <summary>
        /// Retrieves a paginated list of users based on optional parameters.
        /// </summary>
        /// <param name="param">Optional parameters for filtering and pagination (defaults to new UserParams if null).</param>
        /// <returns>A PaginationResponse containing the list of UserDTOs, page size, page index, and total count.</returns>
        public async Task<PaginationResponse<UserDTO>> GetAllUsersAsync(UserParams param = null)
        {
            param ??= new UserParams();
            var spec = new UserSpecification(param);
            var users = await _unitOfWork.Repository<ApplicationUser>()
                .GetAllBySpecAsync(spec);


            var totalCount = await _unitOfWork.Repository<ApplicationUser>()
                .CountAsync(spec);

            var userDtos = users.Select(user => new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FName = user.FirstName,
                LName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                IsDeleted = user.IsDeleted
            }).ToList();
            return new PaginationResponse<UserDTO>
            {
                PageSize = param.PageSize,
                PageIndex = param.PageIndex,
                TotalCount = totalCount,
                Data = userDtos
            };
        }
        #endregion


        #region Get User By Id Async
        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>A UserDTO containing user details if found; otherwise, null.</returns>
        public async Task<UserDTO> GetUserByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return null;
            }

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                FName = user.FirstName,
                LName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                IsDeleted = user.IsDeleted
            };
        }
        #endregion


        #region Update User Async
        /// <summary>
        /// Updates a user's details based on the provided DTO.
        /// </summary>
        /// <param name="userDto">The data transfer object containing updated user information.</param>
        /// <returns>A string indicating success or failure with a relevant message.</returns>
        public async Task<string> UpdateUserAsync(UserDTO userDto)
        {
            if (userDto == null || string.IsNullOrWhiteSpace(userDto.Id))
            {
                return "Invalid user data.";
            }

            var user = await _userManager.FindByIdAsync(userDto.Id);
            if (user == null)
            {
                return "User not found.";
            }

            // Update user properties
            user.FirstName = userDto.FName;
            user.LastName = userDto.LName;
            user.Email = userDto.Email;
            user.UserName = userDto.Email; // Assuming UserName is tied to Email in Identity
            user.PhoneNumber = userDto.PhoneNumber;
            user.Address = userDto.Address;

            // Save changes
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return $"Failed to update user: {errors}";
            }

            return "User updated successfully.";
        }

        #endregion


        #region Toggle User Block Async
        /// <summary>
        /// Toggles a user's block status based on the provided ID and block flag.
        /// </summary>
        /// <param name="id">The ID of the user to block or unblock.</param>
        /// <param name="isBlock">True to block the user, false to unblock.</param>
        /// <returns>A string indicating success or failure with a relevant message.</returns>
        public async Task<string> ToggleUserBlockAsync(string id, bool isBlock)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "Invalid user ID.";
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return "User not found.";
            }

            // Check if the requested action is redundant
            if (isBlock && user.IsDeleted)
            {
                return "User is already blocked.";
            }
            if (!isBlock && !user.IsDeleted)
            {
                return "User is not blocked.";
            }

            user.IsDeleted = isBlock;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return $"Failed to {(isBlock ? "block" : "unblock")} user: {errors}";
            }

            return $"User {(isBlock ? "blocked" : "unblocked")} successfully.";
        }
        #endregion
    }
}


