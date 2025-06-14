using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.ProductDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Settings;
using ECommerce.BL.Specification.ProductSpecification;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Models;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ECommerce.BL.Services.ProductServices
{
    public class ProductServices : IProductServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Cloudinary _cloudinary;
        private readonly string[] _allowedImageExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
        private readonly string[] _allowedVideoExtensions = { ".mp4", ".webm", ".mov", ".mkv" };
        private readonly long _maxAllowedImageSize = 3 * 1024 * 1024;
        private readonly long _maxAllowedVideoSize = 10 * 1024 * 1024;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for database operations.</param>
        /// <param name="imageService">The service for handling image operations.</param>
        public ProductServices(IUnitOfWork unitOfWork, IOptions<CloudinarySettings> cloudinary)
        {
            Account account = new()
            {
                Cloud = cloudinary.Value.Cloud,
                ApiKey = cloudinary.Value.ApiKey,
                ApiSecret = cloudinary.Value.ApiSecret
            };
            _cloudinary = new Cloudinary(account);
            _unitOfWork = unitOfWork;
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
        }


        #region Create Product
        /// <summary>
        /// Creates a new product with the provided data.
        /// </summary>
        /// <param name="dto">The data for creating the product.</param>
        /// <returns>A result indicating the IsSuccess or failure of the operation.</returns>
        public async Task<ResultDTO> CreateProductAsync(CreateProductDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.Price < 0)
                return new ResultDTO { IsSuccess = false, Message = "Invalid product data provided." };

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(dto.CategoryId);
            if (category == null)
                return new ResultDTO { IsSuccess = false, Message = $"Category with ID {dto.CategoryId} not found." };

            string? mainImageUrl = null;
            string? mainImagePublicId = null;

            var type = ResourceType.Image;
            if (dto.MainImage != null)
            {
                var extension = Path.GetExtension(dto.MainImage.FileName).ToLowerInvariant();
                if (_allowedImageExtensions.Contains(extension))
                {
                    if (dto.MainImage.Length > _maxAllowedImageSize)
                        return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 3MB for images." };
                }
                else if (_allowedVideoExtensions.Contains(extension))
                {
                    if (dto.MainImage.Length > _maxAllowedVideoSize)
                        return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                    type = ResourceType.Video;
                }
                else
                    return new ResultDTO { IsSuccess = false, Message = "Invalid file type. Allowed types are: png, jpg, jpeg, webp, svg for images and mp4, webm, mov, mkv for videos." };
                

                var uploadResult = await FileUploader.UploadMediaAsync(dto.MainImage, _cloudinary,type);
                if (!uploadResult.IsSuccess)
                    return new ResultDTO { IsSuccess = false, Message = uploadResult.ErrorMessage };
                
                mainImageUrl = uploadResult.MediaUrl;
                mainImagePublicId = uploadResult.PublicId;
            }

            string? additionalAttributesJson = null;
            if (dto.AdditionalAttributes != null && dto.AdditionalAttributes.Any())
            {
                try
                {
                    additionalAttributesJson = JsonSerializer.Serialize(dto.AdditionalAttributes,_jsonOptions);
                }
                catch
                {
                    return new ResultDTO { IsSuccess = false, Message = "Invalid additional attributes format." };
                }
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                AdditionalAttributes = additionalAttributesJson,
                Status = dto.Status,
                MainImageURL = mainImageUrl,
                ImagePublicId = mainImagePublicId,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<Product>().AddAsync(product);
            var result = await _unitOfWork.Complete();

            if (dto.AdditionalMedia != null && dto.AdditionalMedia.Any())
            {
                foreach (var media in dto.AdditionalMedia)
                {
                    if (media == null || media.Length == 0)
                        continue;
                    var extension = Path.GetExtension(media.FileName).ToLowerInvariant();
                    if (_allowedImageExtensions.Contains(extension))
                    {
                        type = ResourceType.Image;
                        if (media.Length > _maxAllowedImageSize)
                            return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 3MB for images." };
                    }
                    else if (_allowedVideoExtensions.Contains(extension))
                    {
                        type = ResourceType.Video;
                        if (media.Length > _maxAllowedVideoSize)
                                return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                    }
                    else if (extension != ".pdf")
                    {
                        type = ResourceType.Raw;
                        if (media.Length > 1024*1024 )
                            return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                    }
                    else
                        return new ResultDTO { IsSuccess = false, Message = "Invalid file type. Allowed types are: png, jpg, jpeg, webp, svg for images and mp4, webm, mov, mkv for videos." };

                    var uploadResult = await FileUploader.UploadMediaAsync(media, _cloudinary,type);
                    if (!uploadResult.IsSuccess)
                        continue;

                    var ProductMedia = new ProductMedia
                    {
                        MediaURL = uploadResult.MediaUrl,
                        ImageThumbnailURL = uploadResult.ThumbnailUrl,
                        MediaPublicId = uploadResult.PublicId,
                        ProductId = product.Id
                    };

                    await _unitOfWork.Repository<ProductMedia>().AddAsync(ProductMedia);
                }
            }

            result += await _unitOfWork.Complete();
            if(result <= 0)
            {
                await transaction.RollbackAsync();
                return new ResultDTO { IsSuccess = false, Message = "Failed to create product." };
            }
            await transaction.CommitAsync();
            return new ResultDTO { IsSuccess = true, Message = "Product created IsSuccessfully." };
        }
        #endregion


        #region Update Product
        /// <summary>
        /// Updates an existing product with the provided data.
        /// </summary>
        /// <param name="dto">The data for updating the product.</param>
        /// <returns>A result indicating the IsSuccess or failure of the operation.</returns>

        public async Task<ResultDTO> UpdateProductAsync(UpdateProductDTO dto)
        {
            if (dto == null)
            {
                return new ResultDTO { IsSuccess = false, Message = "Invalid product data provided." };
            }

            var product = await _unitOfWork.Repository<Product>().GetBySpecAsync(new ProductSpecification(dto.Id));
            if (product == null)
            {
                return new ResultDTO { IsSuccess = false, Message = $"Product with ID {dto.Id} not found." };
            }

            if (dto.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Repository<Category>().GetByIdAsync(dto.CategoryId.Value);
                if (category == null)
                {
                    return new ResultDTO { IsSuccess = false, Message = $"Category with ID {dto.CategoryId} not found." };
                }
            }

            product.Name = dto.Name ?? product.Name;
            product.Description = dto.Description ?? product.Description;

            if (dto.AdditionalAttributesJson != null)
            {
                try
                {
                    product.AdditionalAttributes = JsonSerializer.Serialize(dto.AdditionalAttributesJson, _jsonOptions);
                }
                catch
                {
                    return new ResultDTO { IsSuccess = false, Message = "Invalid additional attributes format." };
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                product.Status = dto.Status;
            }

            if (dto.CategoryId.HasValue)
            {
                product.CategoryId = dto.CategoryId.Value;
            }

            string? oldImagePublicId = null;
            var type = ResourceType.Image;
            if (dto.MainImage != null)
            {
                var extension = Path.GetExtension(dto.MainImage.FileName).ToLowerInvariant();
                if (_allowedImageExtensions.Contains(extension))
                {
                    if (dto.MainImage.Length > _maxAllowedImageSize)
                        return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 3MB for images." };
                }
                else if (_allowedVideoExtensions.Contains(extension))
                {
                    type = ResourceType.Video;
                    if (dto.MainImage.Length > _maxAllowedVideoSize)
                        return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                }
                else
                    return new ResultDTO { IsSuccess = false, Message = "Invalid file type. Allowed types are: png, jpg, jpeg, webp, svg for images and mp4, webm, mov, mkv for videos." };

                oldImagePublicId = product?.ImagePublicId;
                
                var uploadResult = await FileUploader.UploadMediaAsync(dto.MainImage, _cloudinary, type);
                if (!uploadResult.IsSuccess)
                {
                    return new ResultDTO { IsSuccess = false, Message = uploadResult.ErrorMessage };
                }

                product.MainImageURL = uploadResult.MediaUrl;
                product.ImagePublicId = uploadResult.PublicId;
            }

            if (dto.MediaToDelete != null && dto.MediaToDelete.Any())
            {
                var mediaToDelete = product.ProductMedia
                    .Where(m => dto.MediaToDelete.Contains(m?.MediaPublicId))
                    .ToList();

                await FileUploader.RemoveMediaAsync(_cloudinary, dto.MediaToDelete.ToArray());
                await _unitOfWork.Repository<ProductMedia>().DeleteRangeAsync(mediaToDelete);
            }

            if (dto.AdditionalMedia != null && dto.AdditionalMedia.Any())
            {
                foreach (var media in dto.AdditionalMedia)
                {
                    var extension = Path.GetExtension(media.FileName).ToLowerInvariant();
                    if (_allowedImageExtensions.Contains(extension))
                    {
                        type = ResourceType.Image;
                        if (media.Length > _maxAllowedImageSize)
                            return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 3MB for images." };
                    }
                    else if (_allowedVideoExtensions.Contains(extension))
                    {
                        type = ResourceType.Video;
                        if (media.Length > _maxAllowedVideoSize)
                            return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                    }
                    else if (extension != ".pdf")
                    {
                        type = ResourceType.Raw;
                        if (media.Length > 1024 * 1024)
                            return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                    }
                    else
                        return new ResultDTO { IsSuccess = false, Message = "Invalid file type. Allowed types are: png, jpg, jpeg, webp, svg for images and mp4, webm, mov, mkv for videos." };


                    var uploadResult = await FileUploader.UploadMediaAsync(media, _cloudinary, type);
                    if (!uploadResult.IsSuccess)
                        continue;

                    var ProductMedia = new ProductMedia
                    {
                        MediaURL = uploadResult.MediaUrl,
                        ImageThumbnailURL = uploadResult.ThumbnailUrl,
                        MediaPublicId = uploadResult.PublicId,
                        ProductId = product.Id
                    };

                    await _unitOfWork.Repository<ProductMedia>().AddAsync(ProductMedia);
                }
            }

            await _unitOfWork.Repository<Product>().UpdateAsync(product);
            await _unitOfWork.Complete();

            if (!string.IsNullOrEmpty(oldImagePublicId))
                await FileUploader.RemoveMediaAsync(_cloudinary, oldImagePublicId);

            return new ResultDTO { IsSuccess = true, Message = "Product updated IsSuccessfully." };
        }

        #endregion


        #region Delete Product
        /// <summary>
        /// Deletes a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to delete.</param>
        /// <returns>A result indicating the IsSuccess or failure of the operation.</returns>
        public async Task<ResultDTO> DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Repository<Product>().GetBySpecAsync(new ProductSpecification(id));
            if (product == null)
            {
                return new ResultDTO { IsSuccess = false, Message = $"Product with ID {id} not found." };
            }
            if (product.ProductMedia != null && product.ProductMedia.Any())
            {
                var mediaPublicIds = product.ProductMedia.Select(m => m.MediaPublicId).ToArray();
                await FileUploader.RemoveMediaAsync(_cloudinary, mediaPublicIds);
            }
            if (!string.IsNullOrEmpty(product.ImagePublicId))
                await FileUploader.RemoveMediaAsync(_cloudinary, product.ImagePublicId);

            await _unitOfWork.Repository<Product>().DeleteAsync(product.Id);
            await _unitOfWork.Complete();
            return new ResultDTO { IsSuccess = true, Message = "Product deleted IsSuccessfully." };
        }

        #endregion


        #region Get Product By Id
        /// <summary>
        /// Retrieves a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to retrieve.</param>
        /// <returns>The product DTO.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the product is not found.</exception>
        public async Task<ProductDTO> GetProductByIdAsync(int id)
        {
            var product = await _unitOfWork.Repository<Product>().GetBySpecAsync(new ProductSpecification(id));
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {id} not found.");
            }

            return MapToProductDTO(product);
        }

        #endregion


        #region Get All Products
        /// <summary>
        /// Retrieves all products with optional filtering and pagination.
        /// </summary>
        /// <param name="param">The parameters for filtering and pagination.</param>
        /// <returns>A paginated response containing product DTOs.</returns>
        public async Task<PaginationResponse<ProductDTO>> GetAllProductsAsync(ProductParams param = null)
        {
            param ??= new ProductParams();

            var spec = new ProductSpecification(param);
            var products = await _unitOfWork.Repository<Product>().GetAllBySpecAsync(spec);
            var totalCount = await _unitOfWork.Repository<Product>().CountAsync(spec);

            var productDTOs = products.Select(MapToProductDTO).ToList();

            return new PaginationResponse<ProductDTO>
            {
                PageSize = param.PageSize,
                PageIndex = param.PageIndex,
                TotalCount = totalCount,
                Data = productDTOs
            };
        }
        #endregion


        #region Map Product to ProductDTO
        private ProductDTO MapToProductDTO(Product product)
        {
            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                AdditionalAttributes = product.AdditionalAttributes,
                Status = product.Status,
                MainImageURL = product.MainImageURL,
                ImagePublicId = product.ImagePublicId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                CreatedAt = product.CreatedAt,
                ProductMedia = product.ProductMedia.Select(m => new ProductMediaDTO
                {
                    MediaURL = m.MediaURL,
                    ImageThumbnailURL = m.ImageThumbnailURL,
                    MediaPublicId = m.MediaPublicId
                }).ToList()
            };
        }

        #endregion

    }
}
