using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class FarmingCommitmentService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator) : IFarmingCommitmentService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICodeGenerator _codeGenerator = codeGenerator;
        public async Task<IServiceResult> GetAllBusinessManagerCommitment(Guid userId)
        {
            // Kiểm tra BM có tồn tại không
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => m.UserId == userId,
                asNoTracking: true
            );

            if (manager == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy BusinessManager tương ứng với tài khoản."
                );
            }

            var commitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: fm => fm.IsDeleted != true && fm.ApprovedByNavigation.User.UserId == userId,
                include: fm => fm.
                Include(fm => fm.Plan).
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.ApprovedByNavigation).
                    ThenInclude(fm => fm.User),
                orderBy: fm => fm.OrderBy(fm => fm.CommitmentCode),
                asNoTracking: true
                );

            if (commitments == null || commitments.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<FarmingCommitmentViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var commitmentDto = commitments
                    .Select(commitments => commitments.MapToFarmingCommitmentViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    commitmentDto
                );
            }
        }

        public async Task<IServiceResult> GetAllFarmerCommitment(Guid userId)
        {
            // Kiểm tra Farmer có tồn tại không
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: m => m.UserId == userId,
                asNoTracking: true
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            var commitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: fm => fm.IsDeleted != true && fm.Farmer.User.UserId == userId,
                include: fm => fm.
                Include(fm => fm.Plan).
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.ApprovedByNavigation).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.FarmingCommitmentsDetails),
                orderBy: fm => fm.OrderBy(fm => fm.CommitmentCode),
                asNoTracking: true
                );

            if (commitments == null || commitments.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<FarmingCommitmentViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var commitmentDto = commitments
                    .Select(commitments => commitments.MapToFarmingCommitmentViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    commitmentDto
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid commitmentId)
        {
            // Tìm commitment theo ID
            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                predicate: c =>
                   c.CommitmentId == commitmentId &&
                   !c.IsDeleted,
                include: fm => fm.
                Include(fm => fm.Plan).
                    ThenInclude(fm => fm.CreatedByNavigation).
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.ApprovedByNavigation).
                    ThenInclude(fm => fm.User).
                Include(p => p.FarmingCommitmentsDetails),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy commitment
            if (commitment == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new FarmingCommitmentViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var commitmentDto = commitment.MapToFarmingCommitmentViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    commitmentDto
                );
            }
        }

        //Minh làm thêm cái này để lọc 
        public async Task<IServiceResult> GetAvailableForCropSeason(Guid userId)
        {
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f => f.UserId == userId && !f.IsDeleted,
                asNoTracking: true
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            var available = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: c =>
                    c.FarmerId == farmer.FarmerId &&
                    c.Status == FarmingCommitmentStatus.Active.ToString() &&
                    !c.IsDeleted,
                include: c => c
                .Include(c => c.Plan)
                .Include(c => c.Farmer)
                    .ThenInclude(f => f.User),
                asNoTracking: true
            );

            var dtoList = available.Select(c => c.MapToFarmingCommitmentViewAllDto()).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }



        public async Task<IServiceResult> Create(FarmingCommitmentCreateDto commitmentCreateDto)
        {
            try
            {
                // Tạm thời chưa có validation
                
                //Generate code
                string commitmentCode = await _codeGenerator.GenerateFarmingCommitmentCodeAsync();

                //Map dto to model
                var newCommitment = commitmentCreateDto.MapToFarmingCommitment(commitmentCode);

                //Lấy registration, chỉ lấy cái nào đã được duyệt và chưa có cam kết
                var selectedRegistration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: r => r.RegistrationId == newCommitment.RegistrationId,
                    include: r => r.Include( r => r.Plan),
                    asNoTracking: true);
                if(selectedRegistration == null)
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không tìm được chi tiết phiếu đăng ký"
                    );

                //Tự động map planId và farmerId từ Registration
                newCommitment.PlanId = selectedRegistration.PlanId;
                newCommitment.FarmerId = selectedRegistration.FarmerId;

                //Tự động map giá cả và sản lượng, thời gian từ Registration
                //newCommitment.ConfirmedPrice = registrationDetail.WantedPrice;
                //newCommitment.CommittedQuantity = registrationDetail.EstimatedYield;
                //newCommitment.EstimatedDeliveryStart = registrationDetail.ExpectedHarvestStart;
                //newCommitment.EstimatedDeliveryEnd = registrationDetail.ExpectedHarvestEnd;
                
                // Save data to database
                await _unitOfWork.FarmingCommitmentRepository.CreateAsync(newCommitment);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var commitment = await _unitOfWork.FarmingCommitmentRepository
                        .GetByIdAsync(
                            predicate: p => p.CommitmentId == newCommitment.CommitmentId,
                            include: c => c.
                            Include( p => p.Plan).
                                ThenInclude(fm => fm.CreatedByNavigation).
                            Include(f => f.Farmer).
                                ThenInclude(f => f.User).
                            Include(p => p.Plan).
                                ThenInclude(c => c.CreatedByNavigation).
                            Include(p => p.FarmingCommitmentsDetails),
                            asNoTracking: true
                        );
                    if (commitment == null)
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            Const.WARNING_NO_DATA_MSG,
                            new FarmingCommitmentViewDetailsDto() //Trả về DTO rỗng
                        );
                    var responseDto = commitment.MapToFarmingCommitmentViewDetailsDto();                    

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                    responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> BulkCreate(FarmingCommitmentBulkCreateDto bulkCreateDto)
        {
            try
            {
                var commitments = new List<FarmingCommitment>();

                string commitmentCode = await _codeGenerator.GenerateFarmingCommitmentCodeAsync();
                var count = GeneratedCodeHelpler.GetGeneratedCodeLastNumber(commitmentCode);

                foreach (var dto in bulkCreateDto.FarmingCommitmentCreateDtos)
                {
                    // Các bước này tương tự như create
                    string newCommitmentCode = $"COMMIT-{DateHelper.NowVietnamTime().Year}-{(count):D4}";
                    count++;
                    var commitment = dto.MapToFarmingCommitment(newCommitmentCode);

                    var registrationDetail = await _unitOfWork.CultivationRegistrationsDetailRepository.GetByIdAsync(
                    predicate: r => r.RegistrationId == commitment.RegistrationId,
                    include: r => r.Include(r => r.Registration),
                    asNoTracking: true);

                    if (registrationDetail == null)
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Không tìm được chi tiết phiếu đăng ký"
                        );

                    commitment.PlanId = registrationDetail.PlanDetailId;
                    commitment.FarmerId = registrationDetail.Registration.FarmerId;

                    commitments.Add(commitment);
                }

                // Save all to DB
                await _unitOfWork.FarmingCommitmentRepository.BulkCreateAsync(commitments);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        commitments
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

    }
}
