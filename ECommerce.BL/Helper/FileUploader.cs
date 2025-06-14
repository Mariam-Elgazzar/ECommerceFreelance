using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using ECommerce.BL.DTO.GlobalDTOs;
using Microsoft.AspNetCore.Http;

namespace ECommerce.BL.Helper
{
    public class FileUploader
    {

        #region Upload Media
        /// <summary>
        /// Uploads a media file to Cloudinary and returns the media and thumbnail URLs.
        /// </summary>
        /// <param name="media">The media file to upload.</param>
        /// <param name="cloudinary">The Cloudinary instance for uploading.</param>
        /// <param name="resourceType">The type of resource (e.g., Image or Video).</param>
        /// <returns>A DTO containing IsSuccess status, media URL, thumbnail URL (null for videos), and error message.</returns>
        public static async Task<UploadFileDTO> UploadMediaAsync(IFormFile media, Cloudinary cloudinary, ResourceType resourceType)
        {
            if (media == null)
            {
                return new UploadFileDTO { IsSuccess = false, ErrorMessage = "Media file is required." };
            }

            try
            {
                var extension = Path.GetExtension(media.FileName).ToLowerInvariant();
                var mediaName = $"{Guid.NewGuid()}{extension}";

                using var stream = media.OpenReadStream();
                RawUploadParams uploadParams = new RawUploadParams();
                if (resourceType == ResourceType.Image)
                {
                    uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(mediaName, stream),
                        UseFilename = true
                    };
                } 
                else if (resourceType == ResourceType.Video)
                {
                    uploadParams = new VideoUploadParams
                    {
                        File = new FileDescription(mediaName, stream),
                        UseFilename = true,
                    };
                }
                else if (resourceType == ResourceType.Raw)
                {
                    uploadParams = new RawUploadParams
                    {
                        File = new FileDescription(mediaName, stream),
                        UseFilename = true,
                    };
                }
                else
                {
                    return new UploadFileDTO { IsSuccess = false, ErrorMessage = "Unsupported resource type." };
                }

                var result = await cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                {
                    return new UploadFileDTO { IsSuccess = false, ErrorMessage = $"Error uploading media: {result.Error.Message}" };
                }

                var url = result.SecureUrl.ToString();
                var thumbnailUrl = resourceType == ResourceType.Image ? GetThumbnailUrl(url) : null;

                return new UploadFileDTO 
                { 
                    IsSuccess = true, 
                    MediaUrl = url, 
                    ThumbnailUrl = thumbnailUrl,
                    PublicId = result.PublicId
                };
            }
            catch (Exception ex)
            {
                return new UploadFileDTO { IsSuccess = false, ErrorMessage = $"An error occurred while uploading media: {ex.Message}" };
            }
        }

        #endregion


        #region Get Thumbnail Url

        /// <summary>
        /// Generates a thumbnail URL for an image or GIF by applying Cloudinary transformations.
        /// </summary>
        /// <param name="url">The original media URL from Cloudinary.</param>
        /// <returns>The transformed thumbnail URL.</returns>
        public static string GetThumbnailUrl(string url)
        {
            var separator = "image/upload/";
            var urlParts = url.Split(separator);
            var thumbnailUrl = $"{urlParts[0]}{separator}c_thumb,w_200,g_face/{urlParts[1]}";
            return thumbnailUrl;
        }

        #endregion


        #region Remove Media

        /// <summary>
        /// Deletes multiple images by their public IDs.
        /// </summary>
        /// <param name="publicIds">The public IDs of the images to delete.</param>
        /// <returns>A result indicating the IsSuccess or failure of the operation.</returns>
        public static async Task<ResultDTO> RemoveMediaAsync(Cloudinary _cloudinary, params string[] publicIds)
        {
            if (publicIds == null || !publicIds.Any(id => !string.IsNullOrWhiteSpace(id)))
            {
                return new ResultDTO { IsSuccess = false, Message = "No valid public IDs provided." };
            }

            var validPublicIds = publicIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
            var result = await _cloudinary.DeleteResourcesAsync(validPublicIds);

            if (result.Error != null)
            {
                return new ResultDTO { IsSuccess = false, Message = $"Error removing media: {result.Error.Message}" };
            }

            if (result.Deleted.Any(kv => kv.Value == "not_found"))
            {
                return new ResultDTO { IsSuccess = false, Message = "One or more media items not found." };
            }

            return new ResultDTO { IsSuccess = true, Message = "Media removed IsSuccessfully." };
        }

        #endregion

    }
}
