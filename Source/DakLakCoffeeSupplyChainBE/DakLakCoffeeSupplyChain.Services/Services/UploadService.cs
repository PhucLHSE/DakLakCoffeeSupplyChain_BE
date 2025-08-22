using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Http;
using System;

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

                // Kiểm tra SecureUrl trước khi sử dụng
                if (uploadResult.SecureUrl == null)
                {
                    throw new InvalidOperationException($"Cloudinary upload failed for image file {file.FileName}. SecureUrl is null. Error: {uploadResult.Error?.Message ?? "Unknown error"}");
                }

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

                // Kiểm tra SecureUrl trước khi sử dụng
                if (uploadResult.SecureUrl == null)
                {
                    throw new InvalidOperationException($"Cloudinary upload failed for video file {file.FileName}. SecureUrl is null. Error: {uploadResult.Error?.Message ?? "Unknown error"}");
                }

                return new UploadImageResult
                {
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId,
                    FileType = fileType,
                    FileSize = file.Length
                };
            }
        }

        public async Task<UploadImageResult> UploadContractFileAsync(IFormFile file)
        {
            var fileType = GetFileType(file.ContentType, file.FileName);
            using var stream = file.OpenReadStream();

            switch (fileType)
            {
                case "image":
                    var imageUploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "daklak/contracts/images"
                    };

                    var imageResult = await _cloudinary
                        .UploadAsync(imageUploadParams);

                    // Kiểm tra SecureUrl trước khi sử dụng
                    if (imageResult.SecureUrl == null)
                    {
                        throw new InvalidOperationException($"Cloudinary upload failed for contract image file {file.FileName}. SecureUrl is null. Error: {imageResult.Error?.Message ?? "Unknown error"}");
                    }

                    return new UploadImageResult
                    {
                        Url = imageResult.SecureUrl.ToString(),
                        PublicId = imageResult.PublicId,
                        FileType = fileType,
                        FileSize = file.Length
                    };

                case "video":
                    var videoUploadParams = new VideoUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "daklak/contracts/videos"
                    };

                    var videoResult = await _cloudinary
                        .UploadAsync(videoUploadParams);

                    // Kiểm tra SecureUrl trước khi sử dụng
                    if (videoResult.SecureUrl == null)
                    {
                        throw new InvalidOperationException($"Cloudinary upload failed for contract video file {file.FileName}. SecureUrl is null. Error: {videoResult.Error?.Message ?? "Unknown error"}");
                    }

                    return new UploadImageResult
                    {
                        Url = videoResult.SecureUrl.ToString(),
                        PublicId = videoResult.PublicId,
                        FileType = fileType,
                        FileSize = file.Length
                    };

                case "document":
                    var documentUploadParams = new RawUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "daklak/contracts/documents"
                    };

                    var documentResult = await _cloudinary
                        .UploadAsync(documentUploadParams);

                    // Kiểm tra SecureUrl trước khi sử dụng
                    if (documentResult.SecureUrl == null)
                    {
                        throw new InvalidOperationException($"Cloudinary upload failed for contract document file {file.FileName}. SecureUrl is null. Error: {documentResult.Error?.Message ?? "Unknown error"}");
                    }

                    return new UploadImageResult
                    {
                        Url = documentResult.SecureUrl.ToString(),
                        PublicId = documentResult.PublicId,
                        FileType = fileType,
                        FileSize = file.Length
                    };

                default:
                    throw new ArgumentException($"Loại file không được hỗ trợ: {file.ContentType}");
            }
        }

        private string GetFileType(string contentType, string fileName)
        {
            // Kiểm tra theo ContentType trước
            if (contentType.StartsWith("image/"))
                return "image";

            if (contentType.StartsWith("video/"))
                return "video";

            if (contentType.StartsWith("application/") || contentType.StartsWith("text/"))
                return "document";

            // Fallback: kiểm tra theo extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension))
                return "image";

            if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" }.Contains(extension))
                return "video";

            if (new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" }.Contains(extension))
                return "document";

            return "document";
        }

        public async Task<UploadImageResult> UploadFromUrlAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("fileUrl is empty");

            UploadResult result;
            string fileType;

            // Thử upload như IMAGE
            var img = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(fileUrl),
                Folder = "daklak/contracts/images"
            });

            if (img.Error == null)
            {
                result = img;
                fileType = "image";
            }
            else
            {
                // Thử VIDEO
                var vid = await _cloudinary.UploadAsync(new VideoUploadParams
                {
                    File = new FileDescription(fileUrl),
                    Folder = "daklak/contracts/videos"
                });

                if (vid.Error == null)
                {
                    result = vid;
                    fileType = "video";
                }
                else
                {
                    // Fallback RAW/DOCUMENT
                    var raw = await _cloudinary.UploadAsync(new RawUploadParams
                    {
                        File = new FileDescription(fileUrl),
                        Folder = "daklak/contracts/documents"
                    });

                    if (raw.Error != null)
                        throw new InvalidOperationException(raw.Error.Message ?? "Upload failed");

                    result = raw;
                    fileType = "document";
                }
            }

            var url = result.SecureUrl?.ToString() ?? result.Url?.ToString();

            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException(result.Error?.Message ?? "Cloudinary did not return a URL.");

            return new UploadImageResult
            {
                Url = url,
                PublicId = result.PublicId,
                FileType = fileType,
                FileSize = 0
            };
        }
    }
}
