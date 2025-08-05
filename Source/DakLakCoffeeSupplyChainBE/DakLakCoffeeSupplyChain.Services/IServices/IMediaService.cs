using Microsoft.AspNetCore.Http;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IMediaService
    {
        Task<List<MediaFile>> UploadAndSaveMediaAsync(IEnumerable<IFormFile> files, string relatedEntity, Guid relatedId, string uploadedBy);
        Task<List<MediaFile>> GetMediaByRelatedAsync(string relatedEntity, Guid relatedId);
    }
}
