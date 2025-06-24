using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
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

        public async Task<IServiceResult> GetById(Guid reportId)
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

        public async Task<IServiceResult> CreateGeneralFarmerReports(GeneralFarmerReportCreateDto dto)
        {
            try
            {
                // Kiểm tra ReportType hợp lệ
                if (dto.ReportType != "Crop" && dto.ReportType != "Processing")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Loại báo cáo không hợp lệ (chỉ cho phép 'Crop' hoặc 'Processing')."
                    );
                }

                // Kiểm tra người gửi báo cáo tồn tại
                var reporter = await _unitOfWork.UserAccountRepository.GetByIdAsync(dto.ReportedBy);
                if (reporter == null || reporter.IsDeleted)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Người gửi báo cáo không tồn tại."
                    );
                }

                // Kiểm tra liên kết Crop hoặc Processing theo loại báo cáo
                if (dto.ReportType == "Crop")
                {
                    if (dto.CropProgressId == null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "CropProgressId là bắt buộc với báo cáo loại 'Crop'."
                        );
                    }

                    var crop = await _unitOfWork.CropProgressRepository.GetByIdAsync(dto.CropProgressId.Value);
                    if (crop == null || crop.IsDeleted)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "CropProgressId không tồn tại."
                        );
                    }
                }

                if (dto.ReportType == "Processing")
                {
                    if (dto.ProcessingProgressId == null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "ProcessingProgressId là bắt buộc với báo cáo loại 'Processing'."
                        );
                    }

                    var processing = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(dto.ProcessingProgressId.Value);
                    if (processing == null || processing.IsDeleted)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "ProcessingProgressId không tồn tại."
                        );
                    }
                }

                // Sinh mã báo cáo
                string reportCode = await _codeGenerator.GenerateGeneralFarmerReportCodeAsync();

                // Map DTO → Entity (kèm ReportCode)
                var newCreateGeneralFarmerReports = dto.MapToNewGeneralFarmerReportAsync(reportCode);

                // Gửi vào repository
                await _unitOfWork.GeneralFarmerReportRepository.CreateAsync(newCreateGeneralFarmerReports);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var responseDto = newCreateGeneralFarmerReports.MapToGeneralFarmerReportViewDetailsDto();

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        responseDto
                    );
                }

                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    Const.FAIL_CREATE_MSG
                );
            }
            catch (DbUpdateException dbEx)
            {
                var detailed = dbEx.InnerException?.Message ?? dbEx.Message;

                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    "Không thể tạo báo cáo: " + detailed
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Lỗi hệ thống: " + ex.Message
                );
            }
        }


    }
}
