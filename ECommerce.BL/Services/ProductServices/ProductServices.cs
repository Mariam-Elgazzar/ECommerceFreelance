using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.ProductDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Settings;
using ECommerce.BL.Specification.ProductSpecification;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Models;
using Microsoft.AspNetCore.Http;
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
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return new ResultDTO { IsSuccess = false, Message = "Invalid product data provided." };

            var (categoryExists, category, categoryResult) = await CheckCategoryExistsAsync(dto.CategoryId);
            if (!categoryExists)
                return categoryResult;

            string? mainImageUrl = null;
            string? mainImagePublicId = null;

            if (dto.MainImage != null)
            {
                // Validate MainImage using ValidateFile
                var validationResult = ValidateFile(dto.MainImage, ResourceType.Image);
                if (!validationResult.IsSuccess)
                    return validationResult;

                var uploadResult = await UploadFileAsync(dto.MainImage, ResourceType.Image);
                if (!uploadResult.IsSuccess)
                    return new ResultDTO { IsSuccess = false, Message = uploadResult?.ErrorMessage };

                mainImageUrl = uploadResult.MediaUrl;
                mainImagePublicId = uploadResult.PublicId;
            }

            var serializeResult = SerializeAdditionalAttributes(dto.AdditionalAttributes, out var additionalAttributesJson);
            if (!serializeResult.IsSuccess)
                return serializeResult;

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
                Brand = dto.Brand,
                Modal = dto.Model,
                Quantity = dto.Quantity
            };

            await _unitOfWork.Repository<Product>().AddAsync(product);
            var result = await _unitOfWork.Complete();

            if (dto.AdditionalMedia != null && dto.AdditionalMedia.Any())
            {
                foreach (var media in dto.AdditionalMedia)
                {
                    var validationResult = ValidateFile(media);
                    if (!validationResult.IsSuccess)
                        return validationResult;

                    var uploadResult = await UploadFileAsync(media, (ResourceType)validationResult.Data);
                    if (!uploadResult.IsSuccess)
                        continue;

                    var productMedia = CreateProductMedia(uploadResult, product.Id);
                    await _unitOfWork.Repository<ProductMedia>().AddAsync(productMedia);
                
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
                return new ResultDTO { IsSuccess = false, Message = "Invalid product data provided." };

            var (productExists, product, productResult) = await CheckProductExistsAsync(dto.Id);
            if (!productExists)
                return productResult;

            if (dto.CategoryId.HasValue)
            {
                var (categoryExists, _, categoryResult) = await CheckCategoryExistsAsync(dto.CategoryId.Value);
                if (!categoryExists)
                    return categoryResult;
                product.CategoryId = dto.CategoryId.Value;
            }

            product.Name = dto.Name ?? product.Name;
            product.Description = dto.Description ?? product.Description;
            product.Brand = dto.Brand ?? product.Brand;
            product.Status = dto.Status ?? product.Status;
            product.Modal = dto.Model ?? product.Modal;
            product.Quantity = dto.Quantity ?? product.Quantity;

            if (dto.AdditionalAttributesJson != null)
            {
                var serializeResult = SerializeAdditionalAttributes(dto.AdditionalAttributesJson, out var additionalAttributesJson);
                if (!serializeResult.IsSuccess)
                    return serializeResult;
                product.AdditionalAttributes = additionalAttributesJson;
            }

            string? oldImagePublicId = null;
            if (dto.MainImage != null)
            {
                // Validate MainImage
                var validationResult = ValidateFile(dto.MainImage, ResourceType.Image);
                if (!validationResult.IsSuccess)
                    return validationResult;

                oldImagePublicId = product.ImagePublicId;
                var uploadResult = await UploadFileAsync(dto.MainImage, ResourceType.Image);
                if (!uploadResult.IsSuccess)
                    return new ResultDTO { IsSuccess = false, Message = uploadResult.ErrorMessage };

                product.MainImageURL = uploadResult.MediaUrl;
                product.ImagePublicId = uploadResult.PublicId;
            }

            if (dto.MediaToDelete != null && dto.MediaToDelete.Any())
            {
                var mediaToDelete = product.ProductMedia
                    .Where(m => m != null && dto.MediaToDelete.Equals(m?.MediaPublicId))
                    .ToList();
                await DeleteMediaAsync(mediaToDelete);
                await _unitOfWork.Repository<ProductMedia>().DeleteRangeAsync(mediaToDelete);
            }

            if (dto.AdditionalMedia != null && dto.AdditionalMedia.Any())
            {
                foreach (var media in dto.AdditionalMedia)
                {
                    if (media == null || media.Length == 0)
                        continue;

                    // Validate AdditionalMedia
                    var mediaValidationResult = ValidateFile(media);
                    if (!mediaValidationResult.IsSuccess)
                        return mediaValidationResult;

                    var uploadResult = await UploadFileAsync(media, (ResourceType)mediaValidationResult?.Data);
                    if (!uploadResult.IsSuccess)
                        continue;

                    // Create ProductMedia
                    var productMedia = CreateProductMedia(uploadResult, product.Id);
                    await _unitOfWork.Repository<ProductMedia>().AddAsync(productMedia);
                }
            }

            await _unitOfWork.Repository<Product>().UpdateAsync(product);
            await _unitOfWork.Complete();

            if (!string.IsNullOrEmpty(oldImagePublicId))
                await DeleteMediaAsync(new[] { new ProductMedia { MediaPublicId = oldImagePublicId } });
            
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
            var (productExists, product, productResult) = await CheckProductExistsAsync(id);
            if (!productExists)
                return productResult;

            if (product.ProductMedia != null && product.ProductMedia.Any())
            {
                await DeleteMediaAsync(product.ProductMedia);
            }

            if (!string.IsNullOrEmpty(product.ImagePublicId))
                await DeleteMediaAsync(new[] { new ProductMedia { MediaPublicId = product.ImagePublicId } });
            
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
            var (productExists, product, _) = await CheckProductExistsAsync(id);
            if (!productExists)
                throw new KeyNotFoundException($"Product with ID {id} not found.");

            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                AdditionalAttributes = product.AdditionalAttributes,
                Status = product.Status,
                Quantity = product.Quantity,
                MainImageURL = product.MainImageURL,
                ImagePublicId = product.ImagePublicId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                CreatedAt = product.CreatedAt,
                Brand = product.Brand,
                Model = product.Modal,
                ProductMedia = product.ProductMedia.Select(m => new ProductMediaDTO
                {
                    MediaURL = m.MediaURL,
                    ImageThumbnailURL = m.ImageThumbnailURL,
                    MediaPublicId = m.MediaPublicId,
                    MediaType = _allowedVideoExtensions
                    .Contains(Path.GetExtension(m.MediaURL)
                    .ToLowerInvariant()) ? "video" :
                    _allowedImageExtensions.
                    Contains(Path.GetExtension(m.MediaURL).
                    ToLowerInvariant()) ? "image" :
                    Path.GetExtension(m.MediaURL).ToLowerInvariant() == ".pdf" ?
                    "pdf" : "unknown"
                }).ToList()
            };
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

            var productDTOs = products.Select(product => new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                AdditionalAttributes = product.AdditionalAttributes,
                Status = product.Status,
                Quantity = product.Quantity,
                MainImageURL = product.MainImageURL,
                ImagePublicId = product.ImagePublicId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                CreatedAt = product.CreatedAt,
                Brand = product.Brand,
                Model = product.Modal
            }).ToList();


            return new PaginationResponse<ProductDTO>
            {
                PageSize = param.PageSize,
                PageIndex = param.PageIndex,
                TotalCount = totalCount,
                Data = productDTOs
            };
        }
        #endregion


        #region Helper Methods

        #region Map Product to ProductDTO
        //private ProductDTO MapToProductDTO(Product product)
        //{
        //    return new ProductDTO
        //    {
        //        Id = product.Id,
        //        Name = product.Name,
        //        Description = product.Description,
        //        AdditionalAttributes = product.AdditionalAttributes,
        //        Status = product.Status,
        //        Quantity = product.Quantity,
        //        MainImageURL = product.MainImageURL,
        //        ImagePublicId = product.ImagePublicId,
        //        CategoryId = product.CategoryId,
        //        CategoryName = product.Category?.Name,
        //        CreatedAt = product.CreatedAt,
        //        Brand = product.Brand,
        //        Model = product.Modal,
        //        ProductMedia = product.ProductMedia.Select(m => new ProductMediaDTO
        //        {
        //            MediaURL = m.MediaURL,
        //            ImageThumbnailURL = m.ImageThumbnailURL,
        //            MediaPublicId = m.MediaPublicId,
        //            MediaType = _allowedVideoExtensions
        //            .Contains(Path.GetExtension(m.MediaURL)
        //            .ToLowerInvariant()) ? "video" :
        //            _allowedImageExtensions.
        //            Contains(Path.GetExtension(m.MediaURL).
        //            ToLowerInvariant()) ? "image" :
        //            Path.GetExtension(m.MediaURL).ToLowerInvariant() == ".pdf" ?
        //            "pdf" : "unknown"
        //        }).ToList()
        //    };
        //}

        #endregion


        #region ValidateFile
        /// <summary>
        /// Validates the file type and size, returning the resource type and validation result.
        /// </summary>
        /// <param name="file">The file to validate.</param>
        /// <param name="defaultType">The default resource type if the file is valid.</param>
        /// <returns>A result indicating whether the file is valid and its resource type.</returns>
        private ResultDTO ValidateFile(IFormFile file, ResourceType defaultType = ResourceType.Image)
        {
            if (file == null || file.Length == 0)
                return new ResultDTO { IsSuccess = false, Message = "No file provided." };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            ResourceType type = defaultType;

            if (_allowedImageExtensions.Contains(extension))
            {
                if (file.Length > _maxAllowedImageSize)
                    return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 3MB for images." };
                type = ResourceType.Image;
            }
            else if (_allowedVideoExtensions.Contains(extension))
            {
                if (file.Length > _maxAllowedVideoSize)
                    return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 10MB for videos." };
                type = ResourceType.Video;
            }
            else if (extension == ".pdf")
            {
                if (file.Length > 1024 * 1024)
                    return new ResultDTO { IsSuccess = false, Message = "File size exceeds the maximum allowed size of 1MB for PDFs." };
            }
            else
                return new ResultDTO { IsSuccess = false, Message = "Invalid file type. Allowed types are: png, jpg, jpeg, webp, svg for images; mp4, webm, mov, mkv for videos; pdf for documents." };

            return new ResultDTO { IsSuccess = true, Message = "File is valid.", Data = type };
        }

        #endregion


        #region UploadFileAsync
        /// <summary>
        /// Uploads a file to Cloudinary or server based on its type.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="cloudinary">The Cloudinary instance for uploading media.</param>
        /// <param name="type">The resource type for the file.</param>
        private async Task<UploadFileDTO> UploadFileAsync(IFormFile file, ResourceType type)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".pdf")
                return await FileUploader.UploadMediaToServerAsync(file);
            return await FileUploader.UploadMediaAsync(file, _cloudinary, type);
        }

        #endregion


        #region CreateProductMedia
        /// <summary>
        /// Creates a ProductMedia entity from upload result.
        /// </summary>
        /// <param name="uploadResult">The result of the file upload.</param>
        /// <param name="productId">The ID of the product associated with the media.</param>
        private ProductMedia CreateProductMedia(UploadFileDTO uploadResult, int productId)
        {
            return new ProductMedia
            {
                MediaURL = uploadResult.MediaUrl,
                ImageThumbnailURL = uploadResult.ThumbnailUrl,
                MediaPublicId = uploadResult.PublicId,
                ProductId = productId
            };
        }

        #endregion


        #region RemoveMediaAsync

        /// <summary>
        /// Deletes media files from Cloudinary and server (for PDFs).
        /// </summary>
        /// <param name="mediaItems">The collection of ProductMedia items to delete.</param>
        public async Task DeleteMediaAsync(IEnumerable<ProductMedia> mediaItems)
        {
            if (mediaItems == null || !mediaItems.Any())
                return;

            var pdfMedia = mediaItems.Where(m => !string.IsNullOrEmpty(m.MediaURL) && m.MediaURL.Contains(".pdf")).ToList();
            if (pdfMedia.Any())
            {
                foreach (var media in pdfMedia)
                {
                    FileUploader.RemoveMediaFromServer(media.MediaURL);
                }
            }

            var mediaPublicIds = mediaItems.Select(m => m.MediaPublicId).Where(id => !string.IsNullOrEmpty(id)).ToArray();
            if (mediaPublicIds.Any())
            {
                await FileUploader.RemoveMediaAsync(_cloudinary, mediaPublicIds);
            }
        }

        #endregion


        #region SerializeAdditionalAttributes
        /// <summary>
        /// Serializes additional attributes to JSON string.
        /// </summary>
        /// <param name="additionalAttributes">The additional attributes to serialize.</param>
        /// <param name="additionalAttributesJson">The serialized JSON string of additional attributes.</param>
        /// <returns>A result indicating whether the serialization was successful.</returns>
        public ResultDTO SerializeAdditionalAttributes(object additionalAttributes, out string? additionalAttributesJson)
        {
            additionalAttributesJson = null;
            if (additionalAttributes != null)
            {
                try
                {
                    additionalAttributesJson = JsonSerializer.Serialize(additionalAttributes, _jsonOptions);
                    return new ResultDTO { IsSuccess = true };
                }
                catch
                {
                    return new ResultDTO { IsSuccess = false, Message = "Invalid additional attributes format." };
                }
            }
            return new ResultDTO { IsSuccess = true };
        }

        #endregion


        #region CheckProductExistsAsync
        /// <summary>
        /// Checks if a product exists by ID.
        /// </summary>
        /// <param name="id">The ID of the product to check.</param>
        /// <returns>A tuple indicating whether the product exists, the product itself, and a result DTO.</returns>
        public async Task<(bool Exists, Product Product, ResultDTO Result)> CheckProductExistsAsync(int id)
        {
            var product = await _unitOfWork.Repository<Product>().GetBySpecAsync(new ProductSpecification(id));
            if (product == null)
                return (false, null, new ResultDTO { IsSuccess = false, Message = $"Product with ID {id} not found." });
            return (true, product, new ResultDTO { IsSuccess = true });
        }
        #endregion


        #region CheckCategoryExistsAsync
        /// <summary>
        /// Checks if a category exists by ID.
        /// </summary>
        /// <param name="categoryId">The ID of the category to check.</param>
        public async Task<(bool Exists, Category Category, ResultDTO Result)> CheckCategoryExistsAsync(int categoryId)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId);
            if (category == null)
                return (false, null, new ResultDTO { IsSuccess = false, Message = $"Category with ID {categoryId} not found." });
            return (true, category, new ResultDTO { IsSuccess = true });
        }
        #endregion

        #endregion

    }
}

