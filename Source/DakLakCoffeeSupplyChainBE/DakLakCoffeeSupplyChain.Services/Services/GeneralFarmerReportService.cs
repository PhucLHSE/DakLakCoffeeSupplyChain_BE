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
using Microsoft.AspNetCore.Http;

    namespace DakLakCoffeeSupplyChain.Services.Services
    {
        public class GeneralFarmerReportService : IGeneralFarmerReportService
        {
                    private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;
        private readonly IMediaService _mediaService;

                    public GeneralFarmerReportService(
            IUnitOfWork unitOfWork, 
            ICodeGenerator codeGenerator,
            INotificationService notificationService,
            IMediaService mediaService)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
            
            _notificationService = notificationService
                ?? throw new ArgumentNullException(nameof(notificationService));

            _mediaService = mediaService
                ?? throw new ArgumentNullException(nameof(mediaService));
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

            // ✅ Thêm method cho BusinessManager xem tất cả báo cáo
            public async Task<IServiceResult> GetAllForManagerAsync()
            {
                try
                {
                    var reports = await _unitOfWork.GeneralFarmerReportRepository.GetAllWithIncludesAsync();

                    if (reports == null || !reports.Any())
                        return new ServiceResult(
                            Const.WARNING_NO_DATA_CODE,
                            "Không có báo cáo nào."
                        );

                    var dtoList = reports.Select(r => new GeneralFarmerReportViewForManagerDto
                    {
                        ReportId = r.ReportId,
                        ReportCode = r.ReportCode,
                        Title = r.Title,
                        Description = r.Description,
                        ReportType = r.ReportType,
                        SeverityLevel = r.SeverityLevel,
                        ReportedAt = r.ReportedAt,
                        UpdatedAt = r.UpdatedAt,
                        ResolvedAt = r.ResolvedAt,
                        IsResolved = r.IsResolved,
                        ReportedByName = r.ReportedByNavigation?.Name ?? "Không xác định",
                        ReportedByEmail = r.ReportedByNavigation?.Email ?? "Không xác định",
                        ReportedByPhone = r.ReportedByNavigation?.PhoneNumber ?? "Không xác định",
                        ImageUrl = r.ImageUrl,
                        VideoUrl = r.VideoUrl,
                        ExpertAdviceCount = r.ExpertAdvices?.Where(a => !a.IsDeleted).Count() ?? 0
                    }).ToList();

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
                        // Upload media nếu có
                        if (dto.PhotoFiles?.Any() == true || dto.VideoFiles?.Any() == true)
                        {
                            try
                            {
                                var allMediaFiles = new List<IFormFile>();
                                if (dto.PhotoFiles?.Any() == true)
                                    allMediaFiles.AddRange(dto.PhotoFiles);
                                if (dto.VideoFiles?.Any() == true)
                                    allMediaFiles.AddRange(dto.VideoFiles);

                                // Upload media files
                                var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                                    allMediaFiles,
                                    relatedEntity: "GeneralFarmerReport",
                                    relatedId: newReport.ReportId,
                                    uploadedBy: userId.ToString()
                                );

                                // Cập nhật ImageUrl và VideoUrl từ media đầu tiên của mỗi loại
                                var firstPhoto = mediaList.FirstOrDefault(m => m.MediaType == "image");
                                var firstVideo = mediaList.FirstOrDefault(m => m.MediaType == "video");

                                if (firstPhoto != null || firstVideo != null)
                                {
                                    newReport.ImageUrl = firstPhoto?.MediaUrl;
                                    newReport.VideoUrl = firstVideo?.MediaUrl;
                                    await _unitOfWork.GeneralFarmerReportRepository.UpdateAsync(newReport);
                                    await _unitOfWork.SaveChangesAsync();
                                }
                            }
                            catch (Exception mediaEx)
                            {
                                Console.WriteLine($"WARNING: Media upload failed but report was created successfully: {mediaEx.Message}");
                            }
                        }

                        // ✅ Gửi thông báo cho các chuyên gia
                        await _notificationService.NotifyFarmerReportCreatedAsync(
                            newReport.ReportId, 
                            userId, 
                            newReport.Title
                        );

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
