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
                var uploadResult = await _uploadService.UploadAsync(file);

                var media = new MediaFile
                {
                    MediaId = Guid.NewGuid(),
                    RelatedEntity = relatedEntity,
                    RelatedId = relatedId,
                    MediaType = file.ContentType.StartsWith("video") ? "video" : "image",
                    MediaUrl = uploadResult.Url,
                    Caption = null,
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
    }
}