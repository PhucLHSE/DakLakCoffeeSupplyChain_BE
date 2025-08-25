using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
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
                predicate: fm => fm.IsDeleted != true,
                include: fm => fm.
                Include(fm => fm.Plan).
                    ThenInclude(fm => fm.CreatedByNavigation).
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.FarmingCommitmentsDetails).
                    ThenInclude(fm => fm.PlanDetail).
                        ThenInclude(fm => fm.CoffeeType),
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
                    ThenInclude(fm => fm.CreatedByNavigation).
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.ApprovedByNavigation).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.FarmingCommitmentsDetails).
                    ThenInclude(fm => fm.PlanDetail).
                        ThenInclude(fm => fm.CoffeeType),
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
                Include(p => p.FarmingCommitmentsDetails).
                    ThenInclude(fm => fm.PlanDetail).
                        ThenInclude(fm => fm.CoffeeType),
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

            // Lấy tất cả commitments của farmer
            var allCommitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: c =>
                    c.FarmerId == farmer.FarmerId &&
                    c.Status == FarmingCommitmentStatus.Active.ToString() &&
                    c.ApprovedAt.HasValue && // Chỉ lấy commitments đã được duyệt
                    !c.IsDeleted,
                include: c => c
                .Include(c => c.Plan).
                    ThenInclude(fm => fm.CreatedByNavigation)
                .Include(c => c.Farmer)
                    .ThenInclude(f => f.User)
                .Include(c => c.ApprovedByNavigation)
                .Include(c => c.FarmingCommitmentsDetails).
                    ThenInclude(fm => fm.PlanDetail).
                        ThenInclude(fm => fm.CoffeeType)
                .Include(c => c.FarmingCommitmentsDetails).
                    ThenInclude(fm => fm.RegistrationDetail),
                asNoTracking: true
            );

            // Lọc ra những commitments chưa có crop season
            var availableCommitments = new List<FarmingCommitment>();
            foreach (var commitment in allCommitments)
            {
                var hasCropSeason = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                    cs => cs.CommitmentId == commitment.CommitmentId && !cs.IsDeleted
                );
                
                if (!hasCropSeason)
                {
                    availableCommitments.Add(commitment);
                }
            }

            var dtoList = availableCommitments.Select(c => c.MapToFarmingCommitmentViewAllDto()).ToList();

            // Debug log để kiểm tra ApprovedAt
            foreach (var dto in dtoList)
            {
                Console.WriteLine($"DEBUG: Commitment {dto.CommitmentCode} - ApprovedAt: {dto.ApprovedAt}");
            }

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> Create(FarmingCommitmentCreateDto commitmentCreateDto)
        {
            try
            {

                //Generate code
                string commitmentCode = await _codeGenerator.GenerateFarmingCommitmentCodeAsync();
                string commitmentDetailCode = await _codeGenerator.GenerateFarmingCommitmenstDetailCodeAsync();
                var count = GeneratedCodeHelpler.GetGeneratedCodeLastNumber(commitmentDetailCode);

                //Map dto to model
                var newCommitment = commitmentCreateDto.MapToFarmingCommitment(commitmentCode);

                // Kiểm tra Không cho người dùng chọn trùng chi tiết đăng ký 
                var registrationDetailIds = newCommitment.FarmingCommitmentsDetails.Select(d => d.RegistrationDetailId).ToList();
                if (registrationDetailIds.Count != registrationDetailIds.Distinct().Count())
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không được phép chọn trùng chi tiết đơn đăng ký trong danh sách cam kết."
                    );
                }

                //Lấy registration, chỉ lấy cái nào đã được duyệt và chưa có cam kết
                var selectedRegistration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: r => r.RegistrationId == newCommitment.RegistrationId && r.FarmingCommitment == null,
                    include: r => r.
                        Include(r => r.Plan).
                        Include(r => r.FarmingCommitment).
                        Include(r => r.CultivationRegistrationsDetails),
                    asNoTracking: true);
                if (selectedRegistration == null)
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không tìm được phiếu đăng ký hoặc phiếu đăng ký này đã có cam kết rồi."
                    );

                //Tự động map planId và farmerId từ Registration
                newCommitment.PlanId = selectedRegistration.PlanId;
                newCommitment.FarmerId = selectedRegistration.FarmerId;

                double? registeredQuantity = 0;

                // Tạo Commitment Detail
                foreach (var detail in newCommitment.FarmingCommitmentsDetails)
                {
                    // Kiểm tra đồng bộ dữ liệu, không cho phép lấy registration detail mà không thuộc registration chính
                    var RegistrationDetailsIds = selectedRegistration.CultivationRegistrationsDetails.Select(i => i.CultivationRegistrationDetailId).ToHashSet();
                    if (!RegistrationDetailsIds.Contains(detail.RegistrationDetailId))
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Chi tiết đăng ký được chọn không thuộc phiếu đăng ký đang chọn."
                        );

                    // Lấy planDetailId từ registration detail đã chọn để tự động map vào commitment detail
                    var selectedRegistrationDetail = await _unitOfWork.CultivationRegistrationsDetailRepository.GetByIdAsync(
                        predicate: f => f.CultivationRegistrationDetailId == detail.RegistrationDetailId,
                        include: f => f.
                            Include(f => f.PlanDetail),
                        asNoTracking: true
                        );
                    if (selectedRegistrationDetail == null)
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "selectedRegistrationDetail được chọn bị rỗng"
                        );

                    // Lấy sản lượng đã đăng ký
                    registeredQuantity = selectedRegistrationDetail.PlanDetail.TargetQuantity * 
                        selectedRegistrationDetail.PlanDetail.ProgressPercentage / 100;

                    // Kiểm tra xem sản lượng thống nhất có vượt tổng sản lượng của kế hoạch không
                    //if (detail.CommittedQuantity > registeredQuantity || detail.CommittedQuantity < selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity)
                    //    return new ServiceResult(
                    //        Const.FAIL_CREATE_CODE,
                    //        "Sản lượng thống nhất phải nằm trong phạm vi sản lượng kế hoạch đã đề ra. " +
                    //        $"Cụ thể từ {selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity} kg đến {selectedRegistrationDetail.PlanDetail.TargetQuantity} kg"
                    //    );

                    if (registeredQuantity + selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity < 
                        selectedRegistrationDetail.PlanDetail.TargetQuantity)
                    {
                        if (registeredQuantity + detail.CommittedQuantity > selectedRegistrationDetail.PlanDetail.TargetQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                "Sản lượng thống nhất của chi tiết cam kết này đã vượt quá sản lượng đã đăng ký của chi tiết kế hoạch thu mua. " +
                                $"Sản lượng của chi tiết kế hoạch này là {selectedRegistrationDetail.PlanDetail.TargetQuantity}kg."
                            );
                        }
                        if (detail.CommittedQuantity < selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                "Sản lượng thống nhất phải nằm trong phạm vi tối thiểu của chi tiết kế hoạch. " +
                                $"Cụ thể từ {selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity}kg."
                            );
                        }                        
                    }
                    else
                    {
                        if (detail.CommittedQuantity <= 0 || registeredQuantity + detail.CommittedQuantity > selectedRegistrationDetail.PlanDetail.TargetQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                $"Sản lượng thống nhất phải lớn hơn 0 và không vượt quá phần còn lại: {selectedRegistrationDetail.PlanDetail.TargetQuantity - registeredQuantity}kg."
                            );
                        }
                    }

                    // Kiểm tra xem ngày bắt đầu giao có sau ngày kết thúc thu hoạch không
                    if (detail.EstimatedDeliveryStart < selectedRegistrationDetail.ExpectedHarvestEnd)
                        return new ServiceResult (Const.FAIL_CREATE_CODE,
                            "Ngày dự kiến bắt đầu giao hàng phải sau ngày dự kiến kết thúc thu hoạch. Cụ thể là từ " +
                            $"{selectedRegistrationDetail.ExpectedHarvestEnd}"
                            );


                    detail.CommitmentDetailCode = $"FCD-{DateHelper.NowVietnamTime().Year}-{(count):D4}";
                    count++;
                    detail.PlanDetailId = selectedRegistrationDetail.PlanDetailId;

                    //var confirmedPriceAfterTax = detail.ConfirmedPrice;

                    //Thuế tạm thời bị bỏ qua
                    // Lấy giá trị thuế được set mềm ở bảng systemConfiguration
                    //var taxCode = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    //    predicate: t => t.Name == "TAX_RATE_FOR_COMMITMENT" && !t.IsDeleted,
                    //    asNoTracking: true
                    //    );
                    //if (taxCode != null)
                    //{
                    //    if (detail.ConfirmedPrice.HasValue && taxCode.MinValue.HasValue)
                    //        detail.TaxPrice = detail.ConfirmedPrice.Value * (double)taxCode.MinValue.Value;
                    //    else detail.TaxPrice = 0;

                    //    confirmedPriceAfterTax += detail.TaxPrice;
                    //}


                    newCommitment.TotalPrice += detail.ConfirmedPrice*detail.CommittedQuantity;
                    if (detail.AdvancePayment > newCommitment.TotalPrice)
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Số tiền tạm ứng không được lớn hơn tổng số tiền cam kết."
                        );
                    newCommitment.TotalAdvancePayment += detail.AdvancePayment;
                    //newCommitment.TotalTaxPrice += detail.TaxPrice;
                }

                // Cập nhật lại status của các chi tiết đơn đăng ký không được chọn
                // Lấy các chi tiết vẫn đang là pending vì mặc định các chi tiết đã được chọn sẽ có status là "Approved"
                var unselectedRegistrationDetails = await _unitOfWork.CultivationRegistrationsDetailRepository.GetAllAsync(
                    predicate: r => r.Status == CultivationRegistrationStatus.Pending.ToString() &&
                                    r.RegistrationId == selectedRegistration.RegistrationId &&
                                    !registrationDetailIds.Contains(r.CultivationRegistrationDetailId),
                    asNoTracking: false
                    );
                foreach (var detail in unselectedRegistrationDetails)
                {
                    // Cập nhật status của chi tiết đăng ký không được chọn
                    detail.Status = CultivationRegistrationStatus.Rejected.ToString();
                    await _unitOfWork.CultivationRegistrationsDetailRepository.UpdateAsync(detail);
                }

                // Cập nhật lại status của đơn đăng ký
                var updateSelectedRegistration = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: r => r.RegistrationId == selectedRegistration.RegistrationId,
                    asNoTracking: false
                );
                if (updateSelectedRegistration == null)
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không tìm thấy phiếu đăng ký để cập nhật trạng thái."
                    );
                updateSelectedRegistration.Status = CultivationRegistrationStatus.Approved.ToString();
                await _unitOfWork.CultivationRegistrationRepository.UpdateAsync(updateSelectedRegistration);

                // Save data to database
                await _unitOfWork.FarmingCommitmentRepository.CreateAsync(newCommitment);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var commitment = await _unitOfWork.FarmingCommitmentRepository
                        .GetByIdAsync(
                            predicate: p => p.CommitmentId == newCommitment.CommitmentId,
                            include: c => c.
                            Include(p => p.Plan).
                                ThenInclude(fm => fm.CreatedByNavigation).
                            Include(f => f.Farmer).
                                ThenInclude(f => f.User).
                            Include(p => p.Plan).
                                ThenInclude(c => c.CreatedByNavigation).
                            Include(p => p.FarmingCommitmentsDetails).
                                ThenInclude(fm => fm.PlanDetail).
                                    ThenInclude(fm => fm.CoffeeType),
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

        public async Task<IServiceResult> Update(FarmingCommitmentUpdateDto dto, Guid userId, Guid commitmentId)
        {
            try
            {
                //Lấy commitment
                var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                    predicate: p => p.CommitmentId == commitmentId && !p.IsDeleted,
                    include: p => p.
                        Include(p => p.Plan).
                            ThenInclude(p => p.CreatedByNavigation).
                        Include(p => p.FarmingCommitmentsDetails)
                        );

                if (commitment == null || commitment.Plan.CreatedByNavigation.UserId != userId)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy kế hoạch hoặc không thuộc quyền quản lý."
                    );

                // Map dto to model
                dto.MapToFarmingCommitmentUpdateAPI(commitment);

                // Đồng bộ dữ liệu
                var commitmentDetailsIds = dto.FarmingCommitmentsDetailsUpdateDtos.Select(i => i.CommitmentDetailId).ToHashSet();
                var now = DateHelper.NowVietnamTime();

                // Lấy giá trị thuế được set mềm ở bảng systemConfiguration
                //var taxCode = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                //    predicate: t => t.Name == "TAX_RATE_FOR_COMMITMENT" && !t.IsDeleted,
                //    asNoTracking: true
                //    );

                double? registeredQuantity = 0;

                // Xóa mềm Details
                foreach (var oldItem in commitment.FarmingCommitmentsDetails)
                {
                    if (!commitmentDetailsIds.Contains(oldItem.CommitmentDetailId) && !oldItem.IsDeleted)
                    {
                        //commitment.TotalPrice -= ((oldItem.ConfirmedPrice + oldItem.TaxPrice)*oldItem.CommittedQuantity);
                        commitment.TotalPrice -= oldItem.ConfirmedPrice*oldItem.CommittedQuantity;
                        commitment.TotalAdvancePayment -= oldItem.AdvancePayment;
                       //commitment.TotalTaxPrice -= (oldItem.TaxPrice*oldItem.CommittedQuantity);
                        oldItem.IsDeleted = true;
                        oldItem.UpdatedAt = now;
                        await _unitOfWork.FarmingCommitmentsDetailRepository.UpdateAsync(oldItem);
                    }
                }

                // Cập nhật plan detail đang tồn tại
                foreach (var itemDto in dto.FarmingCommitmentsDetailsUpdateDtos)
                {
                    var existingCommitmentDetails = commitment.FarmingCommitmentsDetails.
                        FirstOrDefault(p => p.CommitmentDetailId == itemDto.CommitmentDetailId && itemDto.CommitmentDetailId != Guid.Empty);

                    //var confirmedPriceAfterTax = itemDto.ConfirmedPrice;
                    //var taxPrice = 0.0;

                    //if (taxCode != null)
                    //{
                    //    if (itemDto.ConfirmedPrice.HasValue && taxCode.MinValue.HasValue)
                    //        taxPrice = itemDto.ConfirmedPrice.Value * (double)taxCode.MinValue.Value;
                    //    confirmedPriceAfterTax += taxPrice;
                    //}

                    // Lấy planDetailId từ registration detail đã chọn để tự động map vào commitment detail
                    var selectedRegistrationDetail = await _unitOfWork.CultivationRegistrationsDetailRepository.GetByIdAsync(
                        predicate: f => f.CultivationRegistrationDetailId == itemDto.RegistrationDetailId,
                        include: f => f.
                            Include(f => f.PlanDetail),
                        asNoTracking: true
                        );
                    if (selectedRegistrationDetail == null)
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "selectedRegistrationDetail được chọn bị rỗng"
                        );

                    registeredQuantity = selectedRegistrationDetail.PlanDetail.TargetQuantity *
                        selectedRegistrationDetail.PlanDetail.ProgressPercentage / 100;

                    // Kiểm tra xem sản lượng thống nhất có vượt tổng sản lượng của kế hoạch không
                    if (registeredQuantity + selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity <
                        selectedRegistrationDetail.PlanDetail.TargetQuantity)
                    {
                        if (registeredQuantity + itemDto.CommittedQuantity > selectedRegistrationDetail.PlanDetail.TargetQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                "Sản lượng thống nhất của chi tiết cam kết này đã vượt quá sản lượng đã đăng ký của chi tiết kế hoạch thu mua. " +
                                $"Sản lượng của chi tiết kế hoạch này là {selectedRegistrationDetail.PlanDetail.TargetQuantity}kg."
                            );
                        }
                        if (itemDto.CommittedQuantity < selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                "Sản lượng thống nhất phải nằm trong phạm vi tối thiểu của chi tiết kế hoạch. " +
                                $"Cụ thể từ {selectedRegistrationDetail.PlanDetail.MinimumRegistrationQuantity}kg."
                            );
                        }
                    }
                    else
                    {
                        if (itemDto.CommittedQuantity <= 0 || registeredQuantity + itemDto.CommittedQuantity > selectedRegistrationDetail.PlanDetail.TargetQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                $"Sản lượng thống nhất phải lớn hơn 0 và không vượt quá phần còn lại: {selectedRegistrationDetail.PlanDetail.TargetQuantity - registeredQuantity}kg."
                            );
                        }
                    }

                        // Kiểm tra xem ngày bắt đầu giao có sau ngày kết thúc thu hoạch không
                        if (itemDto.EstimatedDeliveryStart < selectedRegistrationDetail.ExpectedHarvestEnd)
                        return new ServiceResult(Const.FAIL_CREATE_CODE,
                            "Ngày dự kiến bắt đầu giao hàng phải sau ngày dự kiến kết thúc thu hoạch. Cụ thể là từ " +
                            $"{selectedRegistrationDetail.ExpectedHarvestEnd}"
                            );

                    if (existingCommitmentDetails != null)
                    {
                        //commitment.TotalPrice = commitment.TotalPrice - ((existingCommitmentDetails.ConfirmedPrice + existingCommitmentDetails.TaxPrice)* existingCommitmentDetails.CommittedQuantity) + (confirmedPriceAfterTax * itemDto.CommittedQuantity);
                        commitment.TotalPrice = commitment.TotalPrice - (existingCommitmentDetails.ConfirmedPrice* existingCommitmentDetails.CommittedQuantity) + (itemDto.ConfirmedPrice * itemDto.CommittedQuantity);
                        //commitment.TotalTaxPrice = commitment.TotalTaxPrice - existingCommitmentDetails.TaxPrice + taxPrice;
                        if (itemDto.AdvancePayment > commitment.TotalPrice)
                            return new ServiceResult(
                                Const.FAIL_UPDATE_CODE,
                                "Số tiền tạm ứng không được lớn hơn tổng số tiền cam kết."
                            );
                        //existingCommitmentDetails.TaxPrice = taxPrice != 0 ? taxPrice : existingCommitmentDetails.TaxPrice;
                        commitment.TotalAdvancePayment = commitment.TotalAdvancePayment - existingCommitmentDetails.AdvancePayment + itemDto.AdvancePayment;
                        existingCommitmentDetails.AdvancePayment = itemDto.AdvancePayment;
                        existingCommitmentDetails.RegistrationDetailId = itemDto.RegistrationDetailId != Guid.Empty ? itemDto.RegistrationDetailId : existingCommitmentDetails.RegistrationDetailId;
                        existingCommitmentDetails.ConfirmedPrice = itemDto.ConfirmedPrice != 0 ? itemDto.ConfirmedPrice : existingCommitmentDetails.ConfirmedPrice;
                        existingCommitmentDetails.CommittedQuantity = itemDto.CommittedQuantity.HasValue ? itemDto.CommittedQuantity : existingCommitmentDetails.CommittedQuantity;
                        existingCommitmentDetails.EstimatedDeliveryStart = itemDto.EstimatedDeliveryStart.HasValue ? itemDto.EstimatedDeliveryStart : existingCommitmentDetails.EstimatedDeliveryStart;
                        existingCommitmentDetails.EstimatedDeliveryEnd = itemDto.EstimatedDeliveryEnd.HasValue ? itemDto.EstimatedDeliveryEnd : existingCommitmentDetails.EstimatedDeliveryEnd;
                        existingCommitmentDetails.Note = itemDto.Note.HasValue() ? itemDto.Note : existingCommitmentDetails.Note;
                        existingCommitmentDetails.ContractDeliveryItemId = itemDto.ContractDeliveryItemId != Guid.Empty ? itemDto.ContractDeliveryItemId : existingCommitmentDetails.ContractDeliveryItemId;
                        existingCommitmentDetails.UpdatedAt = now;

                        await _unitOfWork.FarmingCommitmentsDetailRepository.UpdateAsync(existingCommitmentDetails);
                    }

                    // Thêm mới các detail chưa có id
                    if (itemDto.CommitmentDetailId == Guid.Empty || !itemDto.CommitmentDetailId.HasValue)
                    {
                        string commitmentDetailCode = await _codeGenerator.GenerateFarmingCommitmenstDetailCodeAsync();
                        var count = GeneratedCodeHelpler.GetGeneratedCodeLastNumber(commitmentDetailCode);
                        var newDetail = new FarmingCommitmentsDetail
                        {
                            CommitmentDetailId = Guid.NewGuid(),
                            CommitmentDetailCode = $"FCD-{now.Year}-{count:D4}",
                            RegistrationDetailId = itemDto.RegistrationDetailId,
                            ConfirmedPrice = itemDto.ConfirmedPrice,
                            AdvancePayment =  itemDto.AdvancePayment,
                            TaxPrice = 0,
                            CommittedQuantity = itemDto.CommittedQuantity,
                            EstimatedDeliveryStart = itemDto.EstimatedDeliveryStart,
                            EstimatedDeliveryEnd = itemDto.EstimatedDeliveryEnd,
                            Note = itemDto.Note,
                            ContractDeliveryItemId = itemDto.ContractDeliveryItemId,
                        };
                        //if (taxCode != null)
                        //    if (newDetail.ConfirmedPrice.HasValue && taxCode.MinValue.HasValue)
                        //        newDetail.TaxPrice = newDetail.ConfirmedPrice.Value * (double)taxCode.MinValue.Value;
                        count++;
                        //commitment.TotalTaxPrice += newDetail.TaxPrice;
                        //commitment.TotalPrice += (newDetail.ConfirmedPrice + newDetail.TaxPrice)*newDetail.CommittedQuantity;
                        commitment.TotalPrice += newDetail.ConfirmedPrice * newDetail.CommittedQuantity;
                        if (newDetail.AdvancePayment > commitment.TotalPrice)
                            return new ServiceResult(
                                Const.FAIL_UPDATE_CODE,
                                "Số tiền tạm ứng không được lớn hơn tổng số tiền cam kết."
                            );
                        commitment.TotalAdvancePayment += newDetail.AdvancePayment;
                        commitment.FarmingCommitmentsDetails.Add(newDetail);
                    }
                }

                // Save data to database
                await _unitOfWork.FarmingCommitmentRepository.UpdateAsync(commitment);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var updatedCommitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                        predicate: p => p.CommitmentId == p.CommitmentId,
                        include: p => p.
                        Include(fm => fm.Plan).
                            ThenInclude(fm => fm.CreatedByNavigation).
                        Include(fm => fm.Farmer).
                            ThenInclude(fm => fm.User).
                        Include(fm => fm.ApprovedByNavigation).
                            ThenInclude(fm => fm.User).
                        Include(p => p.FarmingCommitmentsDetails).
                            ThenInclude(fm => fm.PlanDetail).
                        ThenInclude(fm => fm.CoffeeType),
                        asNoTracking: true
                        );

                    if (updatedCommitment == null)
                        return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                Const.WARNING_NO_DATA_MSG,
                                new FarmingCommitmentViewDetailsDto() //Trả về DTO rỗng
                            );
                    var responseDto = updatedCommitment.MapToFarmingCommitmentViewDetailsDto();

                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                    responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
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

        public async Task<IServiceResult> UpdateStatusByFarmer(FarmingCommitmentUpdateStatusDto dto, Guid userId, Guid commitmentId)
        {
            try
            {
                
                // Lấy commitment theo ID và kiểm tra quyền truy cập
                var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                    predicate: c => c.CommitmentId == commitmentId && !c.IsDeleted
                );
                if (commitment == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy cam kết tương ứng."
                    );
                }

                // Cập nhật trạng thái và lý do từ DTO
                commitment.Status = dto.Status.ToString();
                commitment.RejectionReason = dto.RejectReason;
                // Nếu trạng thái là Approved thì cập nhật thông tin phê duyệt
                if (dto.Status == FarmingCommitmentStatus.Active)
                    commitment.ApprovedAt = DateTime.UtcNow;

                var commitmentDetails = await _unitOfWork.FarmingCommitmentsDetailRepository.GetAllAsync(
                    predicate: d => d.CommitmentId == commitmentId && !d.IsDeleted
                );
                // Cập nhật trạng thái cho từng chi tiết cam kết
                if (commitmentDetails == null)
                    {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy chi tiết cam kết tương ứng."
                    );
                }
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                            predicate: p => p.PlanId == commitment.PlanId && !p.IsDeleted,
                            include: p => p.Include(p => p.ProcurementPlansDetails)
                        );
                if (plan == null)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy kế hoạch tương ứng."
                    );
                foreach (var detail in commitmentDetails)
                {
                    // Nếu trạng thái là Approved thì cập nhật thông tin phê duyệt
                    if (dto.Status == FarmingCommitmentStatus.Active)
                    {
                        detail.Status = FarmingCommitmentStatus.Active.ToString();
                        var planDetail = plan.ProcurementPlansDetails.FirstOrDefault(pd => pd.PlanDetailsId == detail.PlanDetailId);
                        if (planDetail == null)
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Không tìm thấy chi tiết kế hoạch tương ứng."
                        );
                        // Cập nhật tiến độ sản lượng đăng ký của chi tiết kế hoạch
                        // Tính sản lượng đã đăng ký hiện tại dựa trên ProgressPercentage
                        var oldRegistered = (planDetail.ProgressPercentage / 100f) * planDetail.TargetQuantity;
                        // Cộng confirmedQuantity để có sản lượng đăng ký mới
                        var newRegistered = oldRegistered + detail.CommittedQuantity;
                        // Tính ProgressPercentage mới
                        planDetail.ProgressPercentage = planDetail.TargetQuantity > 0
                            ? newRegistered / planDetail.TargetQuantity * 100
                            : 0;
                        planDetail.UpdatedAt = DateHelper.NowVietnamTime();
                        await _unitOfWork.ProcurementPlanDetailsRepository.UpdateAsync(planDetail);
                    }
                    else if (dto.Status == FarmingCommitmentStatus.Rejected)
                    {
                        detail.Status = FarmingCommitmentStatus.Rejected.ToString();
                        detail.RejectionBy = userId;
                        detail.RejectionAt = DateHelper.NowVietnamTime();
                    }
                    else detail.Status = dto.Status.ToString();
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.FarmingCommitmentsDetailRepository.UpdateAsync(detail);
                }

                // Cập nhật tổng sản lượng đăng ký của kế hoạch
                var activeDetails = plan.ProcurementPlansDetails.Where(pd => !pd.IsDeleted).ToList();
                double? weightedProgressSum = 0;
                foreach (var pd in activeDetails)
                {
                    weightedProgressSum += (pd.TargetQuantity / plan.TotalQuantity) * pd.ProgressPercentage;
                }
                plan.ProgressPercentage = plan.TotalQuantity > 0 ? weightedProgressSum : 0;
                if (plan.ProgressPercentage == 100)
                    plan.Status = ProcurementPlanStatus.Closed.ToString();                
                plan.UpdatedAt = DateHelper.NowVietnamTime();
                await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);

                commitment.UpdatedAt = DateHelper.NowVietnamTime();

                // Cập nhật vào DB
                await _unitOfWork.FarmingCommitmentRepository.UpdateAsync(commitment);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var response = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                    predicate: c => c.CommitmentId == commitmentId && !c.IsDeleted,
                    include: fm => fm.
                        Include(fm => fm.Plan).
                            ThenInclude(fm => fm.CreatedByNavigation).
                        Include(fm => fm.Farmer).
                            ThenInclude(fm => fm.User).
                        Include(fm => fm.ApprovedByNavigation).
                            ThenInclude(fm => fm.User).
                        Include(p => p.FarmingCommitmentsDetails).
                            ThenInclude(fm => fm.PlanDetail).
                                ThenInclude(fm => fm.CoffeeType)
                    );
                    if (response == null)
                    {
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            Const.WARNING_NO_DATA_MSG,
                            new FarmingCommitmentViewDetailsDto() //Trả về DTO rỗng
                        );
                    }

                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                        response.MapToFarmingCommitmentViewDetailsDto()
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
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
