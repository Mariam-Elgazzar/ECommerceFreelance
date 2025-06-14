using ECommerce.BL.DTO.CategoryDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Specification.CategorySpecification;
using ECommerce.BL.Specification.Enums;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(IUnitOfWork unitOfWork, ILogger<CategoriesController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Add Category

        /// <summary>
        /// Adds a new category with an image file.
        /// </summary>
        /// <param name="dto">The data transfer object containing category name, description, and image file.</param>
        /// <returns>A response indicating success or failure of the category addition.</returns>
        /// <remarks>
        /// This endpoint creates a new category with an uploaded image file.
        /// The request must include a valid category name and image file.
        ///
        /// Validation rules for request body (multipart/form-data):
        /// - Name: Required, non-empty string.
        /// - Image: Required, must be a .png, .jpg, or .jpeg file, max 3 MB.
        /// - Description: Optional, string.
        ///
        /// Example Request (multipart/form-data):
        /// ```
        /// Content-Type: multipart/form-data
        /// name: Electronics
        /// description: Gadgets and devices
        /// image: (binary file, e.g., image.jpg)
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the category ID and success message when the category is added successfully.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "categoryId": "12345",
        ///   "message": "Category added successfully"
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request is invalid or the image is not supported.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Only .png, .jpg, .jpeg files are allowed!"
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
        [Route("~/Categories/AddCategory")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateCategory(AddCategoryDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateCategory request with name: {Name}", dto?.Name);
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _unitOfWork.CategoryServices.AddCategoryAsync(dto);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Category addition failed for name: {Name}. Reason: {Message}", dto.Name, result.Message);
                    return BadRequest(new { Message = result.Message });
                }

                _logger.LogInformation("Category added successfully");
                return Ok(new
                {
                    Mesaage = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during category addition for name: {Name}", dto.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while processing your request"
                });
            }
        }

        #endregion


        #region Get All Categories
        /// <summary>
        /// Retrieves categories with filtering, sorting, and pagination.
        /// </summary>
        /// <param name="search">Optional search term for category name.</param>
        /// <param name="description">Optional filter for category description.</param>
        /// <param name="sortProp">Sort property (id, name, description).</param>
        /// <param name="sortDirection">Sort direction (asc, desc).</param>
        /// <param name="pageIndex">The page index (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A paginated response containing category DTOs.</returns>
        /// <response code="200">Returns the paginated list of categories.</response>
        /// <response code="400">Returned when the request is invalid.</response>
        /// <response code="500">Returned when an unexpected server error occurs.</response>
        [HttpGet]
        [Route("~/Categories/GetAllCategories")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")] 
        public async Task<IActionResult> GetCategories([FromQuery] CategoryParams param)
        {
            param ??= new CategoryParams
            {
                PageIndex = param.PageIndex <=1 ? 1 : param.PageIndex,
                PageSize = param.PageSize <= 1 ? 1 : param.PageSize
            };

            
            if (!ModelState.IsValid || param.PageIndex < 1 || param.PageSize < 1)
            {
                _logger.LogWarning("Invalid parameters for GetCategories: pageIndex={PageIndex}, pageSize={PageSize}", param.PageIndex, param.PageSize);
                return BadRequest(new { Message = "Invalid pagination parameters." });
            }

            try
            {
                var response = await _unitOfWork.CategoryServices.GetAllCategoriesAsync(param);
                _logger.LogInformation("Retrieved {Count} categories with pageIndex={PageIndex}", response.Data.Count, param.PageIndex);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories with pageIndex={PageIndex}", param.PageIndex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing your request." });
            }
        }
        #endregion


        #region Get Category By Id

        /// <summary>
        /// Retrieves a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to retrieve.</param>
        /// <returns>The category DTO.</returns>
        /// <response code="200">Returns the category.</response>
        /// <response code="404">Returned when the category is not found.</response>
        /// <response code="500">Returned when an unexpected server error occurs.</response>
        [HttpGet]
        [Route("~/Categories/GetCategoryById/{id}")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                var category = await _unitOfWork.CategoryServices.GetCategoryByIdAsync(id);
                _logger.LogInformation("Retrieved category with ID {Id}", id);
                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Category with ID {Id} not found", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing your request." });
            }
        }

        #endregion


        #region Update Category

        /// <summary>
        /// Updates a category based on the provided DTO.
        /// </summary>
        /// <param name="id">The ID of the category to update.</param>
        /// <param name="category">The category DTO containing updated information.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        /// <response code="200">Returns the result of the update operation.</response>
        /// <response code="400">Returned when the request is invalid or IDs do not match.</response>
        /// <response code="500">Returned when an unexpected server error occurs.</response>
        [HttpPut]
        [Route("~/Categories/UpdateCategory")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateCategory(UpdateCategoryDTO category)
        {
            try
            {
                var result = await _unitOfWork.CategoryServices.UpdateCategoryAsync(category);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to update category with ID {Id}: {Message}", category.Id, result.Message);
                    return BadRequest(new { Message = result.Message });
                }

                _logger.LogInformation("Successfully updated category with ID {Id}", category.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID {Id}", category.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing your request." });
            }
        }

        #endregion


        #region Delete Category

        /// <summary>
        /// Deletes a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        /// <response code="200">Returns the result of the delete operation.</response>
        /// <response code="400">Returned when the category is not found.</response>
        /// <response code="500">Returned when an unexpected server error occurs.</response>
        [HttpDelete]
        [Route("~/Categories/DeleteCategory/{id}")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var result = await _unitOfWork.CategoryServices.DeleteCategoryAsync(id);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete category with ID {Id}: {Message}", id, result.Message);
                    return BadRequest(new { Message = result.Message });
                }

                _logger.LogInformation("Successfully deleted category with ID {Id}", id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing your request." });
            }
        }


        #endregion
    }
}

