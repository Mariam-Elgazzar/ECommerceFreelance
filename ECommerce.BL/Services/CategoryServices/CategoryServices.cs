using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ECommerce.BL.DTO.CategoryDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Settings;
using ECommerce.BL.Specification.CategorySpecification;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Models;
using Microsoft.Extensions.Options;

namespace ECommerce.BL.Services.CategoryServices
{
    public class CategoryServices : ICategoryServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Cloudinary _cloudinary;
        private readonly string[] _allowedImageExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
        private readonly long _maxAllowedImageSize = 3 * 1024 * 1024;
        public CategoryServices(IOptions<CloudinarySettings> cloudinary, IUnitOfWork unitOfWork)
        {
            Account account = new()
            {
                Cloud = cloudinary.Value.Cloud,
                ApiKey = cloudinary.Value.ApiKey,
                ApiSecret = cloudinary.Value.ApiSecret
            };

            _cloudinary = new Cloudinary(account);
            _unitOfWork = unitOfWork;
        }


        #region Add Category

        /// <summary>
        /// Adds a new category with an image file.
        /// </summary>
        /// <param name="dto">The data transfer object containing category name, description, and image file.</param>
        /// <returns>A DTO indicating IsSuccess or failure, a message, and the category ID if IsSuccessful.</returns>
        public async Task<ResultDTO> AddCategoryAsync(AddCategoryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Image is null)
            {
                return new ResultDTO { IsSuccess = false, Message = "Name and Image file are required for the category." };
            }

            var extension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(extension))
            {
                return new ResultDTO { IsSuccess = false, Message = "Only .png, .jpg, .jpeg files are allowed!" };
            }

            if (dto.Image.Length > _maxAllowedImageSize)
            {
                return new ResultDTO { IsSuccess = false, Message = "File cannot be more than 3 MB!" };
            }

            try
            {
                var uploadResult = await FileUploader.UploadMediaAsync(dto.Image, _cloudinary, ResourceType.Image);
                if (!uploadResult.IsSuccess)
                {
                    return new ResultDTO { IsSuccess = false, Message = uploadResult.ErrorMessage };
                }

                var category = new Category
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    ImageURL = uploadResult.MediaUrl,
                    ImageThumbnailURL = uploadResult.ThumbnailUrl,
                    ImagePublicId = uploadResult.PublicId
                };

                
                await _unitOfWork.Repository<Category>().AddAsync(category);
                await _unitOfWork.Complete();

                return new ResultDTO
                {
                    IsSuccess = true,
                    Message = "Category added IsSuccessfully.",
                };
            }
            catch (Exception ex)
            {
                return new ResultDTO { IsSuccess = false, Message = $"An error occurred while adding the category: {ex.Message}" };
            }
        }

        #endregion


        #region Get All Categories

        /// <summary>
        /// Retrieves categories with filtering, sorting, and pagination.
        /// </summary>
        /// <param name="param">The parameters for filtering, sorting, and pagination.</param>
        /// <returns>A paginated response containing category DTOs.</returns>
        public async Task<PaginationResponse<CategoryDTO>> GetAllCategoriesAsync(CategoryParams param = null)
        {
            param ??= new CategoryParams();
            var spec = new CategorySpecification(param);
            var categories = await _unitOfWork.Repository<Category>().GetAllBySpecAsync(spec);
            var totalCount = await _unitOfWork.Repository<Category>().CountAsync(spec);

            var data = categories.Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageURL = c.ImageURL,
                ImageThumbnailURL = c.ImageThumbnailURL,
                ImagePublicId = c?.ImagePublicId
            }).ToList();

            return new PaginationResponse<CategoryDTO>
            {
                PageSize = param.PageSize,
                PageIndex = param.PageIndex,
                TotalCount = totalCount,
                Data = data
            };
        }

        #endregion


        #region Get Category By Id

        /// <summary>
        /// Retrieves a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to retrieve.</param>
        /// <returns>The category DTO.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the category is not found.</exception>
        public async Task<CategoryDTO> GetCategoryByIdAsync(int id)
        {
            var spec = new CategorySpecification(id);
            var category = await _unitOfWork.Repository<Category>().GetBySpecAsync(spec);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {id} not found.");
            }

            return new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageURL = category.ImageURL,
                ImageThumbnailURL = category.ImageThumbnailURL,
                ImagePublicId = category.ImagePublicId
            };
        }

        #endregion


        #region Update Category
        /// <summary>
        /// Updates a category based on the provided DTO, ignoring null values and handling image updates.
        /// </summary>
        /// <param name="category">The category DTO containing updated information.</param>
        /// <returns>A result indicating the IsSuccess or failure of the operation.</returns>
        public async Task<ResultDTO> UpdateCategoryAsync(UpdateCategoryDTO category)
        {
            if (category == null)
            {
                return new ResultDTO { IsSuccess = false, Message = "Category data is invalid or name is empty." };
            }

            var spec = new CategorySpecification(category.Id);
            var existingCategory = await _unitOfWork.Repository<Category>().GetBySpecAsync(spec);

            if (existingCategory == null)
            {
                return new ResultDTO { IsSuccess = false, Message = $"Category with ID {category.Id} not found." };
            }

            existingCategory.Name = category.Name ?? existingCategory.Name;

           
            existingCategory.Description = category.Description ?? existingCategory.Description;

            if (category.Image is not null)
            {
                if (!string.IsNullOrEmpty(existingCategory.ImagePublicId))
                    await FileUploader.RemoveMediaAsync(_cloudinary, existingCategory.ImagePublicId);
               
                var extension = Path.GetExtension(category.Image.FileName).ToLowerInvariant();
               
                if (!_allowedImageExtensions.Contains(extension))
                    return new ResultDTO { IsSuccess = false, Message = "Only .png, .jpg, .jpeg files are allowed!" };

                if (category.Image.Length > _maxAllowedImageSize)
                    return new ResultDTO { IsSuccess = false, Message = "File cannot be more than 3 MB!" };
                
                var uploadResult = await FileUploader.UploadMediaAsync(category.Image, _cloudinary, ResourceType.Image);
                if (!uploadResult.IsSuccess)
                    return new ResultDTO { IsSuccess = false, Message = uploadResult.ErrorMessage };
                
                existingCategory.ImageURL = uploadResult.MediaUrl;
                existingCategory.ImageThumbnailURL = uploadResult.ThumbnailUrl;
                existingCategory.ImagePublicId = uploadResult.PublicId; // Assuming the upload result contains a PublicId
            }

            await _unitOfWork.Repository<Category>().UpdateAsync(existingCategory);
            await _unitOfWork.Complete();

            return new ResultDTO { IsSuccess = true, Message = "Category updated IsSuccessfully." };
        }

        #endregion


        #region Delete Category

        /// <summary>
        /// Deletes a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>A result indicating the IsSuccess or failure of the operation.</returns>
        public async Task<ResultDTO> DeleteCategoryAsync(int id)
        {
            var spec = new CategorySpecification(id);
            var category = await _unitOfWork.Repository<Category>().GetBySpecAsync(spec);

            if (category == null)
            {
                return new ResultDTO { IsSuccess = false, Message = $"Category with ID {id} not found." };
            }
            if (!string.IsNullOrEmpty(category.ImagePublicId))
            {
                var removeResult = await FileUploader.RemoveMediaAsync(_cloudinary, category.ImagePublicId);
                if (!removeResult.IsSuccess)
                {
                    return new ResultDTO { IsSuccess = false, Message = $"Error removing media: {removeResult}" };
                }
            }

            await _unitOfWork.Repository<Category>().DeleteAsync(category.Id);
            await _unitOfWork.Complete();

            return new ResultDTO { IsSuccess = true, Message = "Category deleted IsSuccessfully." };
        }

        #endregion
    }
}
