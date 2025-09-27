using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Numerics;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CultivationRegistrationService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator, INotificationService notify) : ICultivationRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICodeGenerator _codeGenerator = codeGenerator;
        private readonly INotificationService _notify = notify;

        public async Task<IServiceResult> DeleteById(Guid registrationId)
        {
            try
            {
                // Tìm cultivation registration theo ID từ repository
                var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: u => u.RegistrationId == registrationId,
                    asNoTracking: false
                );

                // Nếu không tìm thấy, trả về cảnh báo không có dữ liệu
                if (cultivation == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa khỏi repository
                    await _unitOfWork.CultivationRegistrationRepository.RemoveAsync(cultivation);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> GetAll()
        {

            var cultivationRegistrations = await _unitOfWork.CultivationRegistrationRepository.GetAllAsync(
                predicate: c => c.IsDeleted != true,
                include: c => c.
                Include(c => c.Farmer).ThenInclude(c => c.User),
                orderBy: c => c.OrderByDescending(c => c.CreatedAt),
                asNoTracking: true);

            if (cultivationRegistrations == null || cultivationRegistrations.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CultivationRegistrationViewAllDto>()
                );
            }
            else
            {
                var cultivationRegistrationViewAllDto = cultivationRegistrations
                    .Select(c => c.MapToCultivationRegistrationViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationRegistrationViewAllDto
                );
            }
        }

        public async Task<IServiceResult> GetAllAvailable(Guid planId)
        {
            // Lấy những đơn đăng ký thuộc kế hoạch đang chọn
            var cultivationRegistrations = await _unitOfWork.CultivationRegistrationRepository.GetAllAsync(
                predicate: c => !c.IsDeleted && c.PlanId == planId,
                include: c => c.
                    Include(c => c.Farmer).
                        ThenInclude(c => c.User).
                    Include(c => c.CultivationRegistrationsDetails).
                        ThenInclude(c => c.PlanDetail).
                            ThenInclude(c => c.CoffeeType).
                    Include(c => c.CultivationRegistrationsDetails).
                        ThenInclude(c => c.Crop).
                    Include(c => c.FarmingCommitment),
                orderBy: c => c.OrderBy(c => c.RegistrationCode),
                asNoTracking: true);

            if (cultivationRegistrations == null || cultivationRegistrations.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CultivationRegistrationViewAllAvailableDto>()
                );
            }
            else
            {
                var cultivationRegistrationViewAllDto = cultivationRegistrations
                    .Select(c => c.MapToCultivationRegistrationViewAllAvailableDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationRegistrationViewAllDto
                );
            }
        }
        public async Task<IServiceResult> GetByUserId(Guid userId)
        {
            var cultivationRegistrations = await _unitOfWork.CultivationRegistrationRepository.GetAllAsync(
                predicate: c => !c.IsDeleted && c.Farmer.UserId == userId,
                include: c => c.
                    Include(c => c.Farmer).
                        ThenInclude(c => c.User).
                    Include(c => c.CultivationRegistrationsDetails).
                        ThenInclude(c => c.PlanDetail).
                            ThenInclude(c => c.CoffeeType).
                    Include(c => c.CultivationRegistrationsDetails).
                        ThenInclude(c => c.Crop).
                    Include(c => c.FarmingCommitment),
                orderBy: c => c.OrderBy(c => c.RegistrationCode),
                asNoTracking: true);

            if (cultivationRegistrations == null || cultivationRegistrations.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CultivationRegistrationViewAllAvailableDto>()
                );
            }
            else
            {
                var cultivationRegistrationViewAllDto = cultivationRegistrations
                    .Select(c => c.MapToCultivationRegistrationViewAllAvailableDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationRegistrationViewAllDto
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid registrationId)
        {
            var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                predicate: c => c.RegistrationId == registrationId,
                include: c => c.
                Include(c => c.CultivationRegistrationsDetails).
                    ThenInclude(c => c.PlanDetail).
                        ThenInclude( c => c.CoffeeType).
                Include(c => c.Farmer).
                    ThenInclude(c => c.User),
                asNoTracking: true
                );

            if (cultivation == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new CultivationRegistrationViewSumaryDto()
                );
            }
            else
            {
                var cultivationDto = cultivation.MapToCultivationRegistrationViewSumaryDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationDto
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid registrationId)
        {
            try
            {
                // Tìm cultivation registration theo ID từ repository
                var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: u => u.RegistrationId == registrationId,
                    asNoTracking: false
                );

                // Nếu không tìm thấy cultivation registration, trả về cảnh báo không có dữ liệu
                if (cultivation == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    cultivation.IsDeleted = true;
                    cultivation.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm ở repository
                    await _unitOfWork.CultivationRegistrationRepository.UpdateAsync(cultivation);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> Create(CultivationRegistrationCreateViewDto registrationDto, Guid userId)
        {
            //Check trường hợp plan detail bắt buộc phải thuộc cái plan đang được chọn, ko được chọn plan detail không thuộc plan chính
            try
            {
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    predicate: f => f.UserId == userId,
                    include: f => f.Include(f => f.User),
                    asNoTracking: true
                );
                // Kiểm tra xem farmer có tồn tại không
                if (farmer == null)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy nông dân tương ứng với tài khoản."
                    );

                var planDetailsIds = registrationDto.CultivationRegistrationDetailsCreateViewDto
                    .Select(d => d.PlanDetailId)
                    .ToList();
                if (planDetailsIds.Count != planDetailsIds.Distinct().Count())
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không được phép chọn trùng chi tiết kế hoạch trong danh sách cam kết."
                    );
                }

                // Generate cultivation registration code
                string registrationCode = await _codeGenerator.GenerateCultivationRegistrationCodeAsync(); // ví dụ: "USR-YYYY-####" hoặc Guid, tuỳ bạn

                // Map DTO to Entity
                var newCultivationRegistration = registrationDto.MapToCultivationRegistrationCreateDto(registrationCode, farmer.FarmerId);

                var selectedProcurementPlan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                    predicate: p => p.PlanId == newCultivationRegistration.PlanId,
                    include: p => p.
                    Include(p => p.CultivationRegistrations).
                    Include(p => p.CreatedByNavigation).
                    Include(p => p.ProcurementPlansDetails),
                    asNoTracking: true
                    );

                // Kiểm tra xem plan được chọn có tồn tại và còn mở không
                if (selectedProcurementPlan == null || selectedProcurementPlan.Status != "Open")
                    return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Kế hoạch thu mua được chọn không tồn tại hoặc không còn mở"
                        );

                //Validate crops
                var cropIdsInDetails = newCultivationRegistration.CultivationRegistrationsDetails
                    .Select(d => d.CropId)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();
                if (cropIdsInDetails.Count == 0)
                    return new ServiceResult(
                           Const.WARNING_NO_DATA_CODE,
                           "Không lấy được vùng trồng."
                       );

                var crops = await _unitOfWork.CropRepository.GetAllAsync(
                    predicate: c => cropIdsInDetails.Contains(c.CropId) && c.CreatedBy == farmer.FarmerId && c.IsDeleted != true,
                    asNoTracking: true
                );

                // Lấy tất cả commitments đã được approve mà chưa hoàn thành của farmer (để tính diện tích đang được dùng)
                var approvedCommitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                    predicate: fc => fc.FarmerId == farmer.FarmerId && fc.ApprovedAt != null && !fc.Status.Equals("Completed"),
                    include: fc => fc.Include(fc => fc.FarmingCommitmentsDetails).ThenInclude(fc => fc.RegistrationDetail),
                    asNoTracking: true
                );

                foreach (var cropId in cropIdsInDetails)
                {
                    var crop = crops.Single(c => c.CropId == cropId);
                    // Nếu CropArea có thể null, đảm bảo gán 0
                    double cropArea = crop.CropArea.HasValue ? (double)crop.CropArea.Value : 0.0;

                    // Tổng diện tích đã dùng bởi các commitment đã approve
                    double usedByCommitments = 0.0;
                    if (approvedCommitments != null && approvedCommitments.Count != 0)
                    {
                        usedByCommitments = approvedCommitments
                            .SelectMany(ac => ac.FarmingCommitmentsDetails ?? Enumerable.Empty<FarmingCommitmentsDetail>())
                            .Where(d => d.RegistrationDetail.CropId == cropId)
                            .Sum(d => (double?)d.RegistrationDetail.RegisteredArea ?? 0.0);
                    }

                    // Tổng diện tích đã "đang được giữ" bởi các registration khác (status != Rejected)
                    //double usedByRegistrations = 0.0;
                    //if (existingRegistrations != null && existingRegistrations.Any())
                    //{
                    //    usedByRegistrations = existingRegistrations
                    //        .SelectMany(r => r.CultivationRegistrationsDetails ?? Enumerable.Empty<CultivationRegistrationDetail>())
                    //        .Where(d => d.CropId == cropId)
                    //        .Sum(d => (double?)d.RegisteredArea ?? 0.0);
                    //}

                    double available = cropArea - usedByCommitments;
                    if (available < 0) available = 0;

                    // Diện tích đang yêu cầu trong đơn mới cho crop này
                    double requested = newCultivationRegistration.CultivationRegistrationsDetails
                        .Where(d => d.CropId == cropId)
                        .Sum(d => (double?)d.RegisteredArea ?? 0.0);

                    if (requested > available)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Tổng diện tích đăng ký cho vùng trồng '{crop.FarmName ?? crop.CropId.ToString()}' vượt quá diện tích còn lại.\n " +
                            $"Diện tích vùng trồng: {cropArea}ha, đã sử dụng: {usedByCommitments}ha,\n " +
                            $"Diện tích còn lại: {available}ha, diện tích đang đăng ký: {requested}ha."
                        );
                    }
                }

                // Lấy các plan detail id từ plan mẹ được chọn trong hệ thống
                var planDetailIds = selectedProcurementPlan.ProcurementPlansDetails.Select(d => d.PlanDetailsId);

                //Kiểm tra xem farmer này đã đăng ký kế hoạch thu mua này chưa, nếu đăng ký rồi thì không được phép đăng ký thêm
                //foreach (var application in selectedProcurementPlan.CultivationRegistrations)
                //{
                //    if (application.FarmerId == newCultivationRegistration.FarmerId)
                //        return new ServiceResult(
                //            Const.FAIL_CREATE_CODE,
                //            "Bạn đã đăng ký kế hoạch thu mua này rồi, bạn không thể tạo đơn đăng ký mới nữa, nhưng bạn có thể cập nhật đơn đăng ký đang tồn tại với sản lượng mới hoặc loại cà phê mới."
                //        );
                //}

                // Giới hạn số lần cho phép farmer tạo đăng ký
                var limitCount = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: t => t.Name == "CULTIVATION_REGISTRATION_CREATION_LIMIT" && !t.IsDeleted,
                    asNoTracking: true
                    );

                int creationLimit = 0;
                if (limitCount != null && limitCount.MinValue.HasValue)
                    creationLimit = (int)limitCount.MinValue.Value;
                else creationLimit = int.MaxValue;

                var existingRegistrationCount = await _unitOfWork.CultivationRegistrationRepository.CountAsync(
                    r => r.FarmerId == farmer.FarmerId && r.PlanId == registrationDto.PlanId && !r.Status.Equals("Rejected"));

                if (existingRegistrationCount >= creationLimit)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Bạn chỉ được phép tạo tối đa {creationLimit} đơn đăng ký trên kế hoạch thu mua này." +
                        $"Các đơn bị từ chối sẽ không được tính."
                    );
                }

                // Kiểm tra xem sản lượng đã đăng ký của chi tiết đăng ký có vượt quá sản lượng đã đăng ký của chi tiết kế hoạch thu mua không
                double ? registeredQuantity = 0;
                var planDetailsDict = selectedProcurementPlan.ProcurementPlansDetails.ToDictionary(d => d.PlanDetailsId, d => d);
                foreach (var detail in newCultivationRegistration.CultivationRegistrationsDetails)
                {
                    // Kiểm tra xem plan detail có thuộc plan mẹ không
                    if (!planDetailsDict.TryGetValue(detail.PlanDetailId, out var planDetail))
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Chi tiết kế hoạch thu mua không thuộc kế hoạch chính đã chọn."
                        );
                    }

                    // Kiểm tra xem sản lượng dự kiến có vượt quá kế hoạch hoặc ít hơn mức tối thiểu không
                    if (planDetail == null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Chi tiết kế hoạch thu mua không tồn tại trong kế hoạch chính đã chọn."
                        );
                    }
                    registeredQuantity = planDetail.TargetQuantity*planDetail.ProgressPercentage / 100;
                    
                    // Cho phép đăng ký sản lượng nhỏ hơn mức tối thiểu của chi tiết kế hoạch thu mua nếu sản lượng đã đăng ký gần chạm sản lượng mục tiêu 
                    if (registeredQuantity + planDetail.MinimumRegistrationQuantity < planDetail.TargetQuantity)
                    {
                        if (registeredQuantity + detail.EstimatedYield > planDetail.TargetQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                "Sản lượng đăng ký của chi tiết này đã vượt quá sản lượng đã đăng ký của chi tiết kế hoạch thu mua."
                            );
                        }
                        if (detail.EstimatedYield < planDetail.MinimumRegistrationQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                "Sản lượng dự kiến phải nằm trong phạm vi tối thiểu của chi tiết kế hoạch."
                            );
                        }
                    }
                    else
                    {
                        if (detail.EstimatedYield <= 0 || registeredQuantity + detail.EstimatedYield > planDetail.TargetQuantity)
                        {
                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                $"Sản lượng dự kiến phải lớn hơn 0 và không vượt quá phần còn lại: {planDetail.TargetQuantity - registeredQuantity}kg."
                            );
                        }
                    }

                    // Kiểm tra xem mức giá mong muốn có vượt quá kế hoạch hoặc ít hơn mức tối thiểu không
                    if (detail.WantedPrice < planDetail.MinPriceRange || detail.WantedPrice > planDetail.MaxPriceRange)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Mức giá mong muốn phải nằm trong phạm vi tối thiểu và mục tiêu của chi tiết kế hoạch."
                        );
                    }

                    // Tổng hợp lại toàn bộ các mức giá mong muốn của từng chi tiết đơn
                    newCultivationRegistration.TotalWantedPrice += detail.WantedPrice;

                    // Tổng hợp lại toàn bộ diện tích đăng ký của từng chi tiết đơn
                    newCultivationRegistration.RegisteredArea += detail.RegisteredArea;
                }

                // Tạo người dùng ở repository
                await _unitOfWork.CultivationRegistrationRepository.CreateAsync(newCultivationRegistration);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                //Gửi thông báo cho manager
                await _notify.NotifyManagerNewRegistrationdAsync(
                    selectedProcurementPlan.CreatedByNavigation.UserId,
                    userId,
                    farmer.User.Name,
                    $"{selectedProcurementPlan.Title}"
                    );

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    //var responseDto = newCultivationRegistration.MapToCultivationRegistrationViewSumaryDto();

                    var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                        predicate: c => c.RegistrationId == newCultivationRegistration.RegistrationId,
                        include: c => c.
                        Include(c => c.CultivationRegistrationsDetails).
                            ThenInclude(c => c.PlanDetail).
                                ThenInclude(c => c.CoffeeType).
                        Include(c => c.Farmer).
                        ThenInclude(c => c.User),
                        asNoTracking: true
                        );

                    if (cultivation == null)
                        return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                Const.WARNING_NO_DATA_MSG,
                                new CultivationRegistrationViewSumaryDto() //Trả về DTO rỗng
                            );
                    var responseDto = cultivation.MapToCultivationRegistrationViewSumaryDto();

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

        public async Task<IServiceResult> UpdateStatus(CultivationRegistrationUpdateStatusDto dto, Guid userId, Guid registrationDetailId)
        {
            try
            {
                var registrationDetail = await _unitOfWork.CultivationRegistrationsDetailRepository.GetByIdAsync(
                predicate: c => !c.IsDeleted && c.CultivationRegistrationDetailId == registrationDetailId,
                include: c => c.
                    Include(c => c.Registration).
                        ThenInclude(c => c.Farmer).
                    Include(c => c.PlanDetail).
                        ThenInclude(c => c.Plan)
                );
                if (registrationDetail == null)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đơn đăng ký."
                    );
                
                // Kiểm tra role, nếu là farmer thì chỉ được phép chọn hủy, không cho chọn option khác
                // Farmer khác nếu truy cập được api này vẫn có thể update được nhưng phía UI không cho có support chuyện đó
                // Cách này tối ưu vòng lặp nhưng dở ở logic một xíu
                if (registrationDetail.Registration.Farmer.UserId == userId
                        //&& !registrationDetail.Status.Equals("Cancelled")
                   )
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không có quyền hạn này."
                    );
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    asNoTracking: true
                );

                // Kiểm tra người role, nếu là business manager thì chỉ được phép chọn duyệt hoặc từ chối
                if (manager == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không có quyền hạn này."
                    );
                }

                var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => c.CropId == registrationDetail.CropId && c.IsDeleted != true,
                    asNoTracking: true
                );
                if (crop == null)
                    return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            "Không tìm thấy crop."
                        );

                // Lấy tất cả commitments đã được approve mà chưa hoàn thành của farmer (để tính diện tích đang được dùng)
                var approvedCommitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                    predicate: fc => fc.FarmerId == registrationDetail.Registration.FarmerId && fc.ApprovedAt != null && !fc.Status.Equals("Completed"),
                    include: fc => fc.Include(fc => fc.FarmingCommitmentsDetails).ThenInclude(fc => fc.RegistrationDetail),
                    asNoTracking: true
                );

                double cropArea = crop.CropArea.HasValue ? (double)crop.CropArea.Value : 0.0;

                // Tổng diện tích đã dùng bởi các commitment đã approve
                double usedByCommitments = 0.0;
                if (approvedCommitments != null && approvedCommitments.Count != 0)
                {
                    usedByCommitments = approvedCommitments
                        .SelectMany(ac => ac.FarmingCommitmentsDetails ?? Enumerable.Empty<FarmingCommitmentsDetail>())
                        .Where(d => d.RegistrationDetail.CropId == crop.CropId)
                        .Sum(d => (double?)d.RegistrationDetail.RegisteredArea ?? 0.0);
                }
                double available = cropArea - usedByCommitments;
                if (available < 0) available = 0;

                // Diện tích đang yêu cầu trong đơn mới cho crop này
                double? requested = registrationDetail.RegisteredArea;

                if (requested > available && dto.Status.ToString().Equals("Approved"))
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Tổng diện tích đăng ký cho vùng trồng '{crop.FarmName ?? crop.CropId.ToString()}' vượt quá diện tích còn lại.\n " +
                        $"Diện tích vùng trồng: {cropArea}ha, đã sử dụng: {usedByCommitments}ha,\n " +
                        $"Diện tích còn lại: {available}ha, diện tích đang đăng ký: {requested}ha."
                    );
                }

                // Lấy tổng sản lượng đã được duyệt của các chi tiết đăng ký khác trong cùng một đơn đăng ký
                var totalApprovedYield = await _unitOfWork.CultivationRegistrationsDetailRepository.GetAllQueryable()
                    .Where(c => !c.IsDeleted
                        && c.Status == "Approved"
                        && c.PlanDetail.PlanDetailsId == registrationDetail.PlanDetail.PlanDetailsId)
                    .SumAsync(c => (double?)c.EstimatedYield) ?? 0;

                // Kiểm tra sản lượng đăng ký có vượt sản lượng của kế hoạch đã đề ra không
                if (dto.Status.ToString() == "Approved" && (totalApprovedYield + registrationDetail.EstimatedYield) > registrationDetail.PlanDetail.TargetQuantity)
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Sản lượng dự kiến của chi tiết đơn đăng ký này đã vượt sản lượng đăng ký rồi" +
                        $"\n Cụ thể sản lượng đã duyệt cho chi tiết kế hoạch này là {totalApprovedYield}kg." +
                        $"\n Nếu muốn duyệt chi tiết đăng ký này thì hãy từ chối chi tiết đăng ký khác."
                    );

                // Lấy tất cả chi tiết của đơn đăng ký hiện tại
                var registrationDetails = await _unitOfWork.CultivationRegistrationsDetailRepository.GetAllAsync(
                    predicate: c => !c.IsDeleted && c.RegistrationId == registrationDetail.RegistrationId,
                    asNoTracking: false);

                // Update trạng thái trong bộ nhớ để kiểm tra thay cho lấy lại từ DB
                foreach (var detail in registrationDetails)
                {
                    if (detail.CultivationRegistrationDetailId == registrationDetail.CultivationRegistrationDetailId)
                    {
                        detail.Status = dto.Status.ToString();
                        break;
                    }
                }

                // Nếu toàn bộ chi tiết đăng ký đã bị từ chối thì cập nhật trạng thái đơn đăng ký thành từ chối
                bool allRejected = registrationDetails.All(d => d.Status == "Rejected");

                if (allRejected)
                {
                    var registration = registrationDetail.Registration;

                    if (registration != null)
                    {
                        registration.Status = "Rejected";
                        registration.UpdatedAt = DateHelper.NowVietnamTime();
                        await _unitOfWork.CultivationRegistrationRepository.UpdateAsync(registration);
                    }
                }

                registrationDetail.Status = dto.Status.ToString();
                registrationDetail.UpdatedAt = DateHelper.NowVietnamTime();
                registrationDetail.ApprovedAt = DateHelper.NowVietnamTime();
                registrationDetail.ApprovedBy = manager.ManagerId;

                await _unitOfWork.CultivationRegistrationsDetailRepository.UpdateAsync(registrationDetail);
                var result = await _unitOfWork.SaveChangesAsync();

                // Gửi notification cho farmer
                if (dto.Status.ToString().Equals("Approved"))
                    await _notify.NotifyFarmerApprovedRegistrationAsync(
                    registrationDetail.Registration.Farmer.UserId,
                    userId,
                    manager.CompanyName,
                    $"trong kế hoạch {registrationDetail.PlanDetail.Plan.Title}. Bạn có thể xem trong mục các đơn đã đăng ký kế hoạch." +
                    $"Sau khi doanh nghiệp đã duyệt đơn đăng ký của bạn, họ sẽ tạo cam kết thu mua, bạn hãy vào mục cam kết kế hoạch để đồng" +
                    $"ý hoặc từ chối cam kết của họ."
                    );
                if (dto.Status.ToString().Equals("Rejected"))
                    await _notify.NotifyFarmerRejectedRegistrationAsync(
                        registrationDetail.Registration.Farmer.UserId,
                        userId,
                        manager.CompanyName,
                        $"trong kế hoạch {registrationDetail.PlanDetail.Plan.Title}. Bạn có thể xem trong mục các đơn đã đăng ký kế hoạch."
                        );

                if (result > 0)
                {
                    var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: c => c.RegistrationId == registrationDetail.RegistrationId,
                    include: c => c.
                        Include(c => c.CultivationRegistrationsDetails).
                            ThenInclude(c => c.PlanDetail).
                                ThenInclude(c => c.CoffeeType).
                        Include(c => c.Farmer).
                            ThenInclude(c => c.User),
                    asNoTracking: true
                    );
                    if (cultivation == null)
                    {
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            Const.WARNING_NO_DATA_MSG,
                            new CultivationRegistrationViewSumaryDto()
                        );
                    }
                    else
                    {
                        var cultivationDto = cultivation.MapToCultivationRegistrationViewSumaryDto();

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            Const.SUCCESS_UPDATE_MSG,
                            cultivationDto
                        );
                    }
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
