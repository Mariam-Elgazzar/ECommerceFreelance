using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.ProductDTOs;
using ECommerce.BL.Specification.ProductSpecification;

namespace ECommerce.BL.Services.ProductServices
{
    public interface IProductServices
    {
        /// <summary>
        /// Creates a new product with the provided data.
        /// </summary>
        /// <param name="dto">The data for creating the product.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        Task<ResultDTO> CreateProductAsync(CreateProductDTO dto);
        /// <summary>
        /// Updates an existing product with the provided data.
        /// </summary>
        /// <param name="dto">The data for updating the product.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        Task<ResultDTO> UpdateProductAsync(UpdateProductDTO dto);
        /// <summary>
        /// Deletes a product by its ID (soft delete).
        /// </summary>
        /// <param name="id">The ID of the product to delete.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        Task<ResultDTO> DeleteProductAsync(int id);
        /// <summary>
        /// Retrieves a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to retrieve.</param>
        /// <returns>The product DTO.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the product is not found.</exception>
        Task<ProductDTO> GetProductByIdAsync(int id);
        /// <summary>
        /// Retrieves all products with optional filtering and pagination.
        /// </summary>
        /// <param name="param">The parameters for filtering and pagination.</param>
        /// <returns>A paginated response containing product DTOs.</returns>
        Task<PaginationResponse<ProductDTO>> GetAllProductsAsync(ProductParams param = null);
    }
}
