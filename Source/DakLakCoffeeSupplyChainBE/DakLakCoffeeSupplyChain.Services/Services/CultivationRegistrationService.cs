using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CultivationRegistrationService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator) : ICultivationRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICodeGenerator _codeGenerator = codeGenerator;

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
                orderBy: c => c.OrderBy(c => c.RegistrationCode),
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
                var farmerId = await _unitOfWork.FarmerRepository.GetByPredicateAsync(
                    predicate: f => f.UserId == userId,
                    selector: f => f.FarmerId,
                    asNoTracking: true
                );
                // Kiểm tra xem farmer có tồn tại không
                if (farmerId == Guid.Empty)
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
                var newCultivationRegistration = registrationDto.MapToCultivationRegistrationCreateDto(registrationCode, farmerId);

                var selectedProcurementPlan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                    predicate: p => p.PlanId == newCultivationRegistration.PlanId,
                    include: p => p.
                    Include(p => p.CultivationRegistrations).
                    Include(p => p.ProcurementPlansDetails),
                    asNoTracking: true
                    );

                // Kiểm tra xem plan được chọn có tồn tại không
                if (selectedProcurementPlan == null)
                    return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Kế hoạch thu mua được chọn không tồn tại"
                        );

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

                foreach (var detail in newCultivationRegistration.CultivationRegistrationsDetails)
                {
                    // Kiểm tra xem plan detail có thuộc plan mẹ không
                    if (!planDetailIds.Contains(detail.PlanDetailId))
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Chi tiết kế hoạch thu mua không thuộc kế hoạch chính đã chọn."
                        );
                    }

                    // Kiểm tra xem sản lượng dự kiến có vượt quá kế hoạch hoặc ít hơn mức tối thiểu không
                    var planDetail = selectedProcurementPlan.ProcurementPlansDetails.FirstOrDefault(d => d.PlanDetailsId == detail.PlanDetailId);
                    if (planDetail == null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Chi tiết kế hoạch thu mua không tồn tại trong kế hoạch chính đã chọn."
                        );
                    }
                    if (detail.EstimatedYield < planDetail.MinimumRegistrationQuantity || detail.EstimatedYield > planDetail.TargetQuantity)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Sản lượng dự kiến phải nằm trong phạm vi tối thiểu và mục tiêu của chi tiết kế hoạch."
                        );
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
                }

                // Tạo người dùng ở repository
                await _unitOfWork.CultivationRegistrationRepository.CreateAsync(newCultivationRegistration);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

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
                        ThenInclude(c => c.Farmer)
                );
                if (registrationDetail == null)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đơn đăng ký."
                    );
                
                // Kiểm tra người role, nếu là farmer thì chỉ được phép chọn hủy, không cho chọn option khác
                // Farmer khác nếu truy cập được api này vẫn có thể update được nhưng phía UI không cho có support chuyện đó
                // Cách này tối ưu vòng lặp nhưng dở ở logic một xíu
                if (registrationDetail.Registration.Farmer.UserId == userId
                        && !registrationDetail.Status.Equals("Cancelled"))
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không có quyền hạn này."
                    );
                var businessManagerId = await _unitOfWork.BusinessManagerRepository.GetByPredicateAsync(
                    predicate: u => u.UserId == userId,
                    selector: u => u.ManagerId,
                    asNoTracking: true
                );

                // Kiểm tra người role, nếu là business manager thì chỉ được phép chọn duyệt hoặc từ chối
                if (businessManagerId == Guid.Empty)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không có quyền hạn này."
                    );
                }

                registrationDetail.Status = dto.Status.ToString();
                registrationDetail.UpdatedAt = DateHelper.NowVietnamTime();
                registrationDetail.ApprovedAt = DateHelper.NowVietnamTime();
                registrationDetail.ApprovedBy = businessManagerId;

                await _unitOfWork.CultivationRegistrationsDetailRepository.UpdateAsync(registrationDetail);
                var result = await _unitOfWork.SaveChangesAsync();
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
