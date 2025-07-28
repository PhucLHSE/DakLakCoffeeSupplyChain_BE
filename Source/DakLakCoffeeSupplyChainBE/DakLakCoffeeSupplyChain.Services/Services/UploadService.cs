using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DakLakCoffeeSupplyChain.Common.DTOs.UploadImageResultDR;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class UploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;

        public UploadService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<UploadImageResult> UploadImageAsync(IFormFile file)
        {
            if (_cloudinary == null)
                throw new InvalidOperationException("Cloudinary chưa được cấu hình.");

            if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
                Folder = "uploads"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            return new UploadImageResult
            {
                Url = uploadResult.SecureUrl?.ToString(),
                PublicId = uploadResult.PublicId
            };
        }
        public async Task<UploadImageResult> UploadVideoAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "daklak/videos"
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            return new UploadImageResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }

    }
}
