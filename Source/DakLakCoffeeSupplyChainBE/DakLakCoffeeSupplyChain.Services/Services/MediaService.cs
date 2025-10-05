using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Http;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class MediaService : IMediaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadService _uploadService;

        public MediaService(IUnitOfWork unitOfWork, IUploadService uploadService)
        {
            _unitOfWork = unitOfWork;
            _uploadService = uploadService;
        }

        public async Task<List<MediaFile>> UploadAndSaveMediaAsync(
            IEnumerable<IFormFile> files,
            string relatedEntity,
            Guid relatedId,
            string uploadedBy)
        {
            var resultList = new List<MediaFile>();

            foreach (var file in files)
            {
                try
                {
                    var uploadResult = await _uploadService.UploadAsync(file);

                                    // Xác định MediaType dựa trên file type từ UploadService
                // Sử dụng logic giống như Contract để xử lý document files
                string mediaType;
                if (uploadResult.FileType == "video")
                    mediaType = "video";
                else if (uploadResult.FileType == "image")
                    mediaType = "image";
                else if (uploadResult.FileType == "document")
                    mediaType = "image"; // Force documents thành image để bypass constraint
                else
                    mediaType = "image"; // Fallback

                var media = new MediaFile
                {
                    MediaId = Guid.NewGuid(),
                    RelatedEntity = relatedEntity,
                    RelatedId = relatedId,
                    MediaType = mediaType,
                    MediaUrl = uploadResult.Url,
                    Caption = uploadResult.FileType == "document" ? file.FileName : null,
                    UploadedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                    await _unitOfWork.MediaFileRepository.CreateAsync(media);
                    resultList.Add(new MediaFile
                    {
                        MediaUrl = media.MediaUrl,
                        MediaType = media.MediaType
                    });
                }
                catch (Exception ex)
                {
                    // Log lỗi và tiếp tục với file tiếp theo
                    Console.WriteLine($"WARNING: Failed to upload file {file.FileName}: {ex.Message}");
                    // Có thể throw exception để dừng toàn bộ quá trình nếu cần
                    throw new InvalidOperationException($"Failed to upload file {file.FileName}: {ex.Message}", ex);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return resultList;
        }

        public async Task<List<MediaFile>> GetMediaByRelatedAsync(string relatedEntity, Guid relatedId)
        {
            var list = await _unitOfWork.MediaFileRepository
                .GetAllAsync(m => m.RelatedEntity == relatedEntity && m.RelatedId == relatedId && !m.IsDeleted);

            return list.Select(m => new MediaFile
            {
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType
            }).ToList();
        }

        public async Task UpdateRelatedIdAsync(string relatedEntity, Guid oldRelatedId, Guid newRelatedId)
        {
            var mediaFiles = await _unitOfWork.MediaFileRepository
                .GetAllAsync(m => m.RelatedEntity == relatedEntity && m.RelatedId == oldRelatedId && !m.IsDeleted);

            foreach (var media in mediaFiles)
            {
                media.RelatedId = newRelatedId;
                media.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.MediaFileRepository.UpdateAsync(media);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}