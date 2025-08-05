using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Http;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class UploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;

        public UploadService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<UploadImageResult> UploadAsync(IFormFile file)
        {
            var fileType = file.ContentType.StartsWith("video") ? "video" : "image";

            using var stream = file.OpenReadStream();

            if (fileType == "image")
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "daklak/images"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                return new UploadImageResult
                {
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId,
                    FileType = fileType,
                    FileSize = file.Length
                };
            }
            else
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "daklak/videos"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                return new UploadImageResult
                {
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId,
                    FileType = fileType,
                    FileSize = file.Length
                };
            }
        }
    }
}
