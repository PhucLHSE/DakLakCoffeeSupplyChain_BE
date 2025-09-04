using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IFarmingCommitmentService
    {
        Task<IServiceResult> GetAllBusinessManagerCommitment(Guid userId);
        Task<IServiceResult> GetAllFarmerCommitment(Guid userId);
        Task<IServiceResult> GetById(Guid commitmentId);
        Task<IServiceResult> Create(FarmingCommitmentCreateDto commitment);
        Task<IServiceResult> BulkCreate(FarmingCommitmentBulkCreateDto commitments);
        Task<IServiceResult> GetAvailableForCropSeason(Guid userId);
        Task<IServiceResult> Update(FarmingCommitmentUpdateDto commitmentUpdateDto, Guid userId, Guid commitmentId);
        Task<IServiceResult> UpdateStatusByFarmer(FarmingCommitmentUpdateStatusDto dto, Guid userId, Guid commitmentId);
        Task<IServiceResult> UpdateStatusByManager(FarmingCommitmentUpdateStatusDto dto, Guid userId, Guid commitmentId);

    }
}
