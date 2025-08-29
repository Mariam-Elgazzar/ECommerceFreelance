using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.ProductDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Specification.ProductSpecification;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductsController> _logger;
        public ProductsController(IUnitOfWork unitOfWork, ILogger<ProductsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Create Products
        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="dto">The product data including images.</param>
        /// <returns>The result of the creation operation.</returns>
        /// <response code="201">If the product is created successfully.</response>
        /// <response code="400">If the input data is invalid.</response>
        [HttpPost]
        [Route("~/Products/Create")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ResultDTO>> CreateProduct(CreateProductDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed for CreateProduct: Name={Name}, Errors={Errors}", dto?.Name, errors);
                    return BadRequest(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = $"Validation failed: {errors}"
                    });
                }

                if (dto == null || string.IsNullOrEmpty(dto.Name))
                {
                    _logger.LogWarning("Invalid product data provided for CreateProduct: Name is null or empty");
                    return BadRequest(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "Product name is required."
                    });
                }
                if (dto.AdditionalAttributes is string additionalAttributesJson)
                {
                    try
                    {
                        dto.AdditionalAttributesJson = JsonSerializer.Deserialize<Dictionary<string, string>>(additionalAttributesJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize AdditionalAttributes JSON for UpdateProduct: {Json}", additionalAttributesJson);
                        return BadRequest(new ResultDTO
                        {
                            IsSuccess = false,
                            Message = "Invalid format for AdditionalAttributes."
                        });
                    }
                }

                var result = await _unitOfWork.ProductServices.CreateProductAsync(dto);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Product creation failed for Name={Name}: {Message}", dto.Name, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Successfully created product: Name={Name}", dto.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating product: Name={Name}", dto?.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDTO
                {
                    IsSuccess = false,
                    Message = ex.Message
                });
            }
        }
        #endregion


        #region Update Products
        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="id">The ID of the product to update.</param>
        /// <param name="dto">The updated product data including images.</param>
        /// <returns>The result of the update operation.</returns>
        [HttpPut]
        [Route("~/Products/Update")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ResultDTO>> UpdateProduct([FromQuery] UpdateProductDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = $"Validation failed: {errors}"
                    });
                }
                // Deserialize AdditionalAttributes from JSON string if provided as JSON in the request
                if (dto.AdditionalAttributes is string additionalAttributesJson)
                {
                    try
                    {
                        dto.AdditionalAttributesJson = JsonSerializer.Deserialize<Dictionary<string, string>>(additionalAttributesJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize AdditionalAttributes JSON for UpdateProduct: {Json}", additionalAttributesJson);
                        return BadRequest(new ResultDTO
                        {
                            IsSuccess = false,
                            Message = "Invalid format for AdditionalAttributes."
                        });
                    }
                }
                var result = await _unitOfWork.ProductServices.UpdateProductAsync(dto);
                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Product not found for UpdateProduct");
                        return NotFound(new ResultDTO
                        {
                            IsSuccess = false,
                            Message = result.Message
                        });
                    }

                    _logger.LogWarning("Product update failed for: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Successfully updated product, Name={Name}", dto.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating product");
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDTO
                {
                    IsSuccess = false,
                    Message = ex.Message
                });
            }
        }
        #endregion


        #region Delete Products

        /// <summary>
        /// Deletes a product by its ID (soft delete).
        /// </summary>
        /// <param name="id">The ID of the product to delete.</param>
        /// <returns>The result of the deletion operation.</returns>
        /// <response code="200">If the product is deleted successfully.</response>
        /// <response code="404">If the product is not found.</response>
        [HttpDelete]
        [Route("~/Products/Delete/{id}")]
        //[Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ResultDTO>> DeleteProduct(int id)
        {
            try
            {
                if (id < 1)
                {
                    _logger.LogWarning("Invalid product ID provided for DeleteProduct: Id={Id}", id);
                    return BadRequest(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "Product ID must be a positive integer."
                    });
                }

                var result = await _unitOfWork.ProductServices.DeleteProductAsync(id);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Product deletion failed for Id={Id}: {Message}", id, result.Message);
                    return NotFound(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = result.Message
                    });
                }

                _logger.LogInformation("Successfully deleted product: Id={Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting product: Id={Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDTO
                {
                    IsSuccess = false,
                    Message = ex.Message
                });
            }
        }

        #endregion


        #region Get Products

        /// <summary>
        /// Retrieves all products with optional filtering and pagination.
        /// <param name="param">The parameters for filtering and pagination.</param>
        /// <returns>A paginated response containing product DTOs.</returns>
        /// <response code="200">If the products are retrieved successfully.</response>
        [HttpGet]
        [Route("~/Products/GetAllProducts")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")]
        public async Task<ActionResult<PaginationResponse<ProductDTO>>> GetAllProducts([FromQuery] ProductParams param = null)
        {
            try
            {
                // Apply default pagination if param is null
                param ??= new ProductParams { PageIndex = 1, PageSize = 10 };

                if (param.PageIndex < 1 || param.PageSize < 1)
                {
                    _logger.LogWarning("Invalid pagination parameters for GetAllProducts: PageIndex={PageIndex}, PageSize={PageSize}", param.PageIndex, param.PageSize);
                    return BadRequest(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "PageIndex and PageSize must be positive integers."
                    });
                }

                _logger.LogInformation("Retrieving products with parameters: Search={Search}, CategoryId={CategoryId}, PageIndex={PageIndex}, PageSize={PageSize}",
                    param.Search, param.CategoryId, param.PageIndex, param.PageSize);

                var products = await _unitOfWork.ProductServices.GetAllProductsAsync(param);
                if (products == null || !products.Data.Any())
                {
                    _logger.LogWarning("No products found for parameters: Search={Search}, CategoryId={CategoryId}", param.Search, param.CategoryId);
                    return NotFound(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "No products found."
                    });
                }

                _logger.LogInformation("Successfully retrieved {ProductCount} products", products.Data.Count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with parameters: Search={Search}, CategoryId={CategoryId}", param?.Search, param?.CategoryId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDTO
                {
                    IsSuccess = false,
                    Message = ex.Message
                });
            }
        }

        #endregion


        #region Get Product By Id

        /// <summary>
        /// Retrieves a product by its ID.
        /// <param name="id">The ID of the product to retrieve.</param>
        /// <returns>The product DTO.</returns>
        /// <response code="200">If the product is retrieved successfully.</response>
        /// <response code="404">If the product is not found.</response>
        [HttpGet]
        [Route("~/Products/GetById/{id}")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")]
        public async Task<ActionResult<ProductDTO>> GetProductById(int id)
        {
            try
            {
                if (id < 1)
                {
                    _logger.LogWarning("Invalid product ID provided for GetProductById: Id={Id}", id);
                    return BadRequest(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "Product ID must be a positive integer."
                    });
                }

                _logger.LogInformation("Retrieving product: Id={Id}", id);
                var product = await _unitOfWork.ProductServices.GetProductByIdAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found: Id={Id}", id);
                    return NotFound(new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "Product not found."
                    });
                }

                _logger.LogInformation("Successfully retrieved product: Id={Id}, Name={Name}", id, product.Name);
                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found: Id={Id}", id);
                return NotFound(new ResultDTO
                {
                    IsSuccess = false,
                    Message = "Product not found."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving product: Id={Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDTO
                {
                    IsSuccess = false,
                    Message = ex.Message
                });
            }
        }
        #endregion

    }
}
