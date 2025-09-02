//using DakLakCoffeeSupplyChain.Common.DTOs.UploadImageResultDR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;


namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IUploadService
    {
        Task<UploadImageResult> UploadAsync(IFormFile file);

        Task<UploadImageResult> UploadContractFileAsync(IFormFile file);
        Task<UploadImageResult> UploadSettlementFileAsync(IFormFile file);

        Task<UploadImageResult> UploadFromUrlAsync(string fileUrl);
    }
}
