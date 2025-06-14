using ECommerce.BL.DTO.CategoryDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.Specification.CategorySpecification;

namespace ECommerce.BL.Services.CategoryServices
{
    public interface ICategoryServices
    {
        /// <summary>
        /// Adds a new category with an image file.
        /// </summary>
        /// <param name="dto">The data transfer object containing category name, description, and image file.</param>
        /// <returns>A DTO indicating success or failure, a message, and the category ID if successful.</returns>
        Task<ResultDTO> AddCategoryAsync(AddCategoryDTO dto);
        /// <summary>
        /// Retrieves all categories asynchronously with optional filtering and sorting.
        /// </summary>
        /// <param name="param">The parameters for filtering, sorting, and pagination.</param>
        /// <returns>A list of category DTOs.</returns>
        Task<PaginationResponse<CategoryDTO>> GetAllCategoriesAsync(CategoryParams param = null);
        /// <summary>
        /// Retrieves a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to retrieve.</param>
        /// <returns>The category DTO.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the category is not found.</exception>
        Task<CategoryDTO> GetCategoryByIdAsync(int id);
        /// <summary>
        /// Updates a category based on the provided DTO.
        /// </summary>
        /// <param name="category">The category DTO containing updated information.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        Task<ResultDTO> UpdateCategoryAsync(UpdateCategoryDTO category);
        /// <summary>
        /// Deletes a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        Task<ResultDTO> DeleteCategoryAsync(int id);
    }
}
