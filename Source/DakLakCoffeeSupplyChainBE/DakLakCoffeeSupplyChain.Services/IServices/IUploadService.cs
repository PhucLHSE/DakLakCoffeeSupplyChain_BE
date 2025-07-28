using DakLakCoffeeSupplyChain.Common.DTOs.UploadImageResultDR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IUploadService
    {
        Task<UploadImageResult> UploadImageAsync(IFormFile file);
        Task<UploadImageResult> UploadVideoAsync(IFormFile file);
    }
}
