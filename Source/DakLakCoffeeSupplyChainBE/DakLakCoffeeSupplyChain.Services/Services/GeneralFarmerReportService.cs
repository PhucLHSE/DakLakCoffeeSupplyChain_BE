    using DakLakCoffeeSupplyChain.Common;
    using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
    using DakLakCoffeeSupplyChain.Common.Enum.GeneralReportEnums;
    using DakLakCoffeeSupplyChain.Common.Helpers;
    using DakLakCoffeeSupplyChain.Repositories.Base;
    using DakLakCoffeeSupplyChain.Repositories.Models;
    using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
    using DakLakCoffeeSupplyChain.Services.Base;
    using DakLakCoffeeSupplyChain.Services.Generators;
    using DakLakCoffeeSupplyChain.Services.IServices;
    using DakLakCoffeeSupplyChain.Services.Mappers;
    using Microsoft.EntityFrameworkCore;

    namespace DakLakCoffeeSupplyChain.Services.Services
    {
        public class GeneralFarmerReportService : IGeneralFarmerReportService
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICodeGenerator _codeGenerator;

            public GeneralFarmerReportService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
            {
                _unitOfWork = unitOfWork
                    ?? throw new ArgumentNullException(nameof(unitOfWork));

                _codeGenerator = codeGenerator
                    ?? throw new ArgumentNullException(nameof(codeGenerator));
            }

            public async Task<IServiceResult> GetAll()
            {
                try
                {
                    var reports = await _unitOfWork.GeneralFarmerReportRepository.GetAllWithIncludesAsync();

                    if (reports == null || !reports.Any())
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Không có báo cáo nào."
                        );

                    var dtoList = reports.Select(r => r.MapToGeneralFarmerReportViewAllDto()).ToList();

                    return new ServiceResult(
                        Const.SUCCESS_READ_CODE,
                        Const.SUCCESS_READ_MSG,
                        dtoList
                    );
                }
                catch (Exception ex)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
                }
            }

            public async Task<IServiceResult> GetById(Guid reportId)
            {
                try
                {
                    var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdWithIncludesAsync(reportId);

                    if (report == null)
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Không tìm thấy báo cáo."
                         );

                    var dto = report.MapToGeneralFarmerReportViewDetailsDto();

                    return new ServiceResult(
                        Const.SUCCESS_READ_CODE,
                        Const.SUCCESS_READ_MSG,
                        dto
                     );
                }
                catch (Exception ex)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
                }
            }

            public async Task<IServiceResult> CreateGeneralFarmerReports(GeneralFarmerReportCreateDto dto, Guid userId)
            {
                try
                {
                    if (dto.ReportType != ReportTypeEnum.Crop && dto.ReportType != ReportTypeEnum.Processing)
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Loại báo cáo không hợp lệ.");

                    if (dto.ReportType == ReportTypeEnum.Crop)
                    {
                        if (dto.CropProgressId == null)
                            return new ServiceResult(Const.FAIL_CREATE_CODE, "CropProgressId là bắt buộc.");

                        var crop = await _unitOfWork.CropProgressRepository.GetByIdAsync(dto.CropProgressId.Value);
                        if (crop == null || crop.IsDeleted)
                            return new ServiceResult(Const.FAIL_CREATE_CODE, "CropProgressId không tồn tại.");
                    }

                    if (dto.ReportType == ReportTypeEnum.Processing)
                    {
                        if (dto.ProcessingProgressId == null)
                            return new ServiceResult(Const.FAIL_CREATE_CODE, "ProcessingProgressId là bắt buộc.");

                        var processing = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(dto.ProcessingProgressId.Value);
                        if (processing == null || processing.IsDeleted)
                            return new ServiceResult(Const.FAIL_CREATE_CODE, "ProcessingProgressId không tồn tại.");
                    }

                    var reporter = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                    if (reporter == null || reporter.IsDeleted)
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Người dùng không tồn tại.");

                    string reportCode = await _codeGenerator.GenerateGeneralFarmerReportCodeAsync();
                    var newReport = dto.MapToNewGeneralFarmerReportAsync(reportCode, userId);

                    await _unitOfWork.GeneralFarmerReportRepository.CreateAsync(newReport);
                    var result = await _unitOfWork.SaveChangesAsync();

                    if (result > 0)
                    {
                        var responseDto = newReport.MapToGeneralFarmerReportViewDetailsDto();
                        return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
                    }

                    return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                }
                catch (DbUpdateException dbEx)
                {
                    var detailed = dbEx.InnerException?.Message ?? dbEx.Message;
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không thể tạo báo cáo: " + detailed);
                }
                catch (Exception ex)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
                }
            }

            public async Task<IServiceResult> UpdateGeneralFarmerReport(GeneralFarmerReportUpdateDto dto)
            {
                try
                {
                    // 1. Tìm báo cáo theo ID
                    var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdAsync(dto.ReportId);
                    if (report == null || report.IsDeleted)
                    {
                        return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy báo cáo cần cập nhật.");
                    }

                    // 2. Ánh xạ các trường cập nhật từ DTO → Entity
                    dto.MapToUpdatedReport(report);

                    // 3. Cập nhật vào repository
                    _unitOfWork.GeneralFarmerReportRepository.Update(report);

                    // 4. Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    if (result > 0)
                    {
                        var fullReport = await _unitOfWork.GeneralFarmerReportRepository
         .GetByIdWithIncludesAsync(dto.ReportId);

                        if (fullReport == null)
                            return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật xong nhưng không truy xuất được dữ liệu.");

                        var updatedDto = fullReport.MapToGeneralFarmerReportViewDetailsDto();
                        return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, updatedDto);

                    }

                    return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
                }
                catch (Exception ex)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
                }
            }
            public async Task<IServiceResult> SoftDeleteGeneralFarmerReport(Guid reportId)
            {
                try
                {
                    var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdEvenIfDeletedAsync(reportId);
                    if (report == null || report.IsDeleted)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy báo cáo cần xóa.");

                    report.IsDeleted = true;
                    report.UpdatedAt = DateHelper.NowVietnamTime();

                    _unitOfWork.GeneralFarmerReportRepository.Update(report);

                    var result = await _unitOfWork.SaveChangesAsync();
                    if (result > 0)
                        return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xóa mềm báo cáo thành công.");

                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa mềm thất bại.");
                }
                catch (Exception ex)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
                }
            }
            public async Task<IServiceResult> HardDeleteGeneralFarmerReport(Guid reportId)
            {
                try
                {
                    var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdEvenIfDeletedAsync(reportId);
                    if (report == null)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy báo cáo.");

                    await _unitOfWork.GeneralFarmerReportRepository.RemoveAsync(report);
                    var result = await _unitOfWork.SaveChangesAsync();

                    if (result > 0)
                        return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xóa vĩnh viễn báo cáo.");
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa vĩnh viễn thất bại.");
                }
                catch (Exception ex)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
                }
            }
        public async Task<IServiceResult> ResolveGeneralFarmerReportAsync(Guid reportId, Guid expertId)
        {
            var report = await _unitOfWork.GeneralFarmerReportRepository.GetByIdAsync(reportId);
            if (report == null || report.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy báo cáo.");

            if (report.IsResolved == true)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Báo cáo đã được xử lý.");

            var expert = await _unitOfWork.UserAccountRepository.GetByIdAsync(expertId);
            if (expert == null || expert.IsDeleted)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Chuyên gia không tồn tại.");
                
            if (expert.RoleId != 5)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Người xử lý không hợp lệ.");
            }

            // Cập nhật trạng thái xử lý
            report.IsResolved = true;
            report.ResolvedAt = DateHelper.NowVietnamTime();
            report.UpdatedAt = DateHelper.NowVietnamTime(); // cập nhật luôn thời điểm sửa đổi

            _unitOfWork.GeneralFarmerReportRepository.Update(report);
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                var updated = await _unitOfWork.GeneralFarmerReportRepository.GetByIdWithIncludesAsync(reportId);
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Báo cáo đã được xử lý.", updated?.MapToGeneralFarmerReportViewDetailsDto());
            }

            return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không thể cập nhật trạng thái báo cáo.");
        }

    }
}
