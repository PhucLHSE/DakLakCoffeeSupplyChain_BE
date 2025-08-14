using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingBatchService : IProcessingBatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ProcessingBatchService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
        }

        private bool HasPermissionToAccess(ProcessingBatch batch, Guid userId, bool isAdmin, bool isManager)
        {
            if (isAdmin || isManager) return true;
            return batch.Farmer?.UserId == userId;
        }

        public async Task<IServiceResult> GetAll()
        {
            var batches = await _unitOfWork.ProcessingBatchRepository.GetAll();

            if (batches == null || !batches.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingBatchViewDto>()
                );
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }
        public async Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin, bool isManager, bool isExpert = false)
        {
            List<ProcessingBatch> batches;

            if (isAdmin)
            {
                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)  // ← Bỏ ThenInclude(cs => cs.Commitment)
                        .Include(x => x.Farmer).ThenInclude(f => f.User),  // ← Bỏ include ProcessingBatchProgresses
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );
            }
            else if (isManager)
            {
                var manager = await _unitOfWork.BusinessManagerRepository
                    .GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);

                if (manager == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Business Manager tương ứng.");

                var managerId = manager.ManagerId;

                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x =>
                        !x.IsDeleted &&
                        x.CropSeason != null &&
                        x.CropSeason.Commitment != null &&
                        x.CropSeason.Commitment.ApprovedBy == managerId,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)  // ← Bỏ ThenInclude(cs => cs.Commitment)
                        .Include(x => x.Farmer).ThenInclude(f => f.User),  // ← Bỏ include ProcessingBatchProgresses
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (batches == null || !batches.Any())
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập bất kỳ lô sơ chế nào.");
                }
            }
            else if (isExpert)
            {
                // AgriculturalExpert có thể xem tất cả batches để đánh giá
                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)
                        .Include(x => x.Farmer).ThenInclude(f => f.User),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (batches == null || !batches.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có lô sơ chế nào để đánh giá.");
                }
            }
            else
            {
                // Kiểm tra xem user có phải là Farmer không
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);

                if (farmer == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");
                }

                batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: x => !x.IsDeleted && x.FarmerId == farmer.FarmerId,
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)
                        .Include(x => x.Farmer).ThenInclude(f => f.User),
                    asNoTracking: true
                );

                if (batches == null || !batches.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Bạn chưa tạo lô sơ chế nào.");
                }
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }
        public async Task<IServiceResult> CreateAsync(ProcessingBatchCreateDto dto, Guid userId)
        {
            // 1. Không cho BusinessManager tạo lô
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);
            if (manager != null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Business Manager không được tạo lô sơ chế.");

            // 2. Kiểm tra vai trò Farmer
            var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ nông hộ mới được tạo lô sơ chế.");

            // 3. Kiểm tra mùa vụ
            var cropSeason = await _unitOfWork.CropSeasonRepository.GetByIdAsync(dto.CropSeasonId);
            if (cropSeason == null || cropSeason.IsDeleted)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Mùa vụ không hợp lệ.");

            // 4. Kiểm tra phương pháp sơ chế
            var method = await _unitOfWork.ProcessingMethodRepository.GetByIdAsync(dto.MethodId);
            if (method == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Phương pháp sơ chế không hợp lệ.");

            // 5. Truy xuất CropSeasonDetail khớp mùa vụ, loại cà phê và farmer, đã hoàn thành
            var cropDetails = await _unitOfWork.CropSeasonDetailRepository
            .GetAllAsync(cd =>
                !cd.IsDeleted &&
                cd.CropSeasonId == dto.CropSeasonId &&
                cd.Status == "Completed" &&
                cd.CommitmentDetail != null &&
                cd.CommitmentDetail.Commitment != null &&
                cd.CommitmentDetail.PlanDetail != null &&
                cd.CommitmentDetail.PlanDetail.CoffeeTypeId == dto.CoffeeTypeId &&
                cd.CommitmentDetail.Commitment.FarmerId == farmer.FarmerId,
                include: q => q
                    .Include(cd => cd.CommitmentDetail)
                        .ThenInclude(d => d.PlanDetail)
                    .Include(cd => cd.CommitmentDetail)
                        .ThenInclude(d => d.Commitment)
            );

            var cropDetail = cropDetails.FirstOrDefault();

            if (cropDetail == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Loại cà phê không thuộc kế hoạch nào đã hoàn thành.");

            // 6. Kiểm tra khối lượng đầu ra kỳ vọng
            var actualYield = cropDetail.ActualYield ?? 0;
            if (actualYield <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Loại cà phê này chưa có khối lượng đầu ra.");

            // 7. Tính khối lượng còn lại chưa tạo lô
            var existingBatches = await _unitOfWork.ProcessingBatchRepository
                .GetAllAsync(pb => pb.CropSeasonId == dto.CropSeasonId
                                && pb.CoffeeTypeId == dto.CoffeeTypeId
                                && !pb.IsDeleted);

            var usedQuantity = existingBatches.Sum(pb => pb.InputQuantity);
            var remainingQuantity = actualYield - usedQuantity;

            if (remainingQuantity <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Khối lượng của loại cà phê này đã được sử dụng hết.");

            // 8. Sinh mã hệ thống
            int year = cropSeason.StartDate?.Year ?? DateTime.Now.Year;
            string systemBatchCode = await _codeGenerator.GenerateProcessingSystemBatchCodeAsync(year);

            // 9. Tạo lô sơ chế
            var batch = new ProcessingBatch
            {
                BatchId = Guid.NewGuid(),
                BatchCode = dto.BatchCode?.Trim(),
                SystemBatchCode = systemBatchCode,
                CropSeasonId = dto.CropSeasonId,
                CoffeeTypeId = dto.CoffeeTypeId,
                MethodId = dto.MethodId,
                InputQuantity = remainingQuantity,
                InputUnit = "kg",
                FarmerId = farmer.FarmerId,
                Status = ProcessingStatus.NotStarted.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.ProcessingBatchRepository.CreateAsync(batch);
            var saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo lô sơ chế thất bại.");

            // 10. Trả về kết quả
            var created = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                x => x.BatchId == batch.BatchId,
                include: q => q
                    .Include(x => x.Method)
                    .Include(x => x.CropSeason)
                    .Include(x => x.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );

            var dtoResult = created?.MapToProcessingBatchViewDto();
            return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, dtoResult);
        }


        public async Task<IServiceResult> GetAvailableCoffeeTypesAsync(Guid userId, Guid cropSeasonId)
        {
            // Lấy thông tin nông hộ từ UserId
            // B1: Lấy tất cả detail hợp lệ
            var details = await _unitOfWork.CropSeasonDetailRepository.GetAllAsync(
                d => !d.IsDeleted
                    && d.CropSeasonId == cropSeasonId
                    && d.Status == "Completed",
                include: q => q
                    .Include(d => d.CommitmentDetail)
                        .ThenInclude(cd => cd.PlanDetail)
                            .ThenInclude(pd => pd.CoffeeType)
                    .Include(d => d.CommitmentDetail)
                        .ThenInclude(cd => cd.Commitment)
            );

            // B2: Lấy farmerId
            var farmer = await _unitOfWork.FarmerRepository
                .GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
            if (farmer == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy nông hộ.");
            var farmerId = farmer.FarmerId;

            // B3: Lấy các batch đang InProgress của farmer trong crop season này
            var inProgressBatchCoffeeTypeIds = (await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                b => !b.IsDeleted &&
                     b.CropSeasonId == cropSeasonId &&
                     b.FarmerId == farmerId &&
                    b.Status == ProcessingStatus.InProgress.ToString()
            )).Select(b => b.CoffeeTypeId).Distinct().ToHashSet();

            // B4: Chỉ giữ detail nếu coffeeTypeId của nó chưa bị tạo batch InProgress
            var filteredDetails = details
                .Where(d =>
                    d.CommitmentDetail?.Commitment?.FarmerId == farmerId &&
                    d.CommitmentDetail?.PlanDetail?.CoffeeType != null &&
                    !inProgressBatchCoffeeTypeIds.Contains(d.CommitmentDetail.PlanDetail.CoffeeTypeId)
                )
                .ToList();

            // B5: Convert thành CoffeeTypeViewAllDto
            var result = filteredDetails
                .Select(d => d.CommitmentDetail.PlanDetail.CoffeeType)
                .DistinctBy(ct => ct.CoffeeTypeId)
                .Select(ct => new CoffeeTypeViewAllDto
                {
                    CoffeeTypeId = ct.CoffeeTypeId,
                    TypeCode = ct.TypeCode,
                    TypeName = ct.TypeName,
                    BotanicalName = ct.BotanicalName,
                    Description = ct.Description,
                    TypicalRegion = ct.TypicalRegion,
                    SpecialtyLevel = ct.SpecialtyLevel
                }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy thành công", result);
        }


        public async Task<IServiceResult> UpdateAsync(ProcessingBatchUpdateDto dto, Guid userId, bool isAdmin, bool isManager)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId);
            if (batch == null || batch.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");

            if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật lô sơ chế này.");

            batch.CoffeeTypeId = dto.CoffeeTypeId;
            batch.CropSeasonId = dto.CropSeasonId;
            batch.MethodId = dto.MethodId;
            batch.InputQuantity = dto.InputQuantity;
            batch.InputUnit = dto.InputUnit?.Trim();
            batch.Status = dto.Status.ToString();
            batch.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công.")
                : new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại.");
        }

        public async Task<IServiceResult> SoftDeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager)
        {
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                predicate: b => b.BatchId == id && !b.IsDeleted,
                include: q => q.Include(b => b.Farmer).ThenInclude(f => f.User)
            );

            if (batch == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Lô sơ chế không tồn tại.");

            if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá lô sơ chế này.");

            batch.IsDeleted = true;
            batch.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: x => x.BatchId == batchId && !x.IsDeleted,
                    include: q => q.Include(x => x.Farmer).ThenInclude(f => f.User)
                );

                if (batch == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mẻ sơ chế.");

                if (!HasPermissionToAccess(batch, userId, isAdmin, isManager))
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Không được xóa mẻ sơ chế của người khác.");

                await _unitOfWork.ProcessingBatchRepository.RemoveAsync(batch);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Đã xóa vĩnh viễn mẻ sơ chế.")
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> GetFullDetailsAsync(
         Guid batchId,
         Guid userId,
         bool isAdmin,
         bool isManager,
         bool isExpert = false)
        {
            try
            {
                var batchQuery = _unitOfWork.ProcessingBatchRepository
                    .GetQueryable()
                    .Include(b => b.Farmer).ThenInclude(f => f.User)
                    .Include(b => b.CropSeason)
                    .Include(b => b.Method)
                    .Include(b => b.CoffeeType)
                    .Include(b => b.ProcessingBatchProgresses)
                        .ThenInclude(p => p.Stage)
                    .Include(b => b.ProcessingBatchProgresses)
                        .ThenInclude(p => p.ProcessingBatchWastes)
                    .Where(b => !b.IsDeleted && b.BatchId == batchId);

                if (!isAdmin)
                {
                    if (isManager)
                    {
                        var manager = await _unitOfWork.BusinessManagerRepository
                            .GetByIdAsync(b => b.UserId == userId && !b.IsDeleted);
                        if (manager == null)
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Business Manager.");

                        var managerId = manager.ManagerId;
                        batchQuery = batchQuery.Where(b =>
                            b.CropSeason != null &&
                            b.CropSeason.Commitment != null &&
                            b.CropSeason.Commitment.ApprovedBy == managerId
                        );
                    }
                    else if (isExpert)
                    {
                        // Expert có thể xem tất cả batches không bị xóa
                        // Không cần filter thêm
                    }
                    else
                    {
                        batchQuery = batchQuery.Where(b => b.Farmer.UserId == userId);
                    }
                }

                var batch = await batchQuery.FirstOrDefaultAsync();

                if (batch == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");

                // ✅ Lấy toàn bộ UpdatedById từ progresses
                var updatedByIds = batch.ProcessingBatchProgresses
                    .Where(p => p.UpdatedBy != null)
                    .Select(p => p.UpdatedBy)
                    .Distinct()
                    .ToList();

                // ✅ Lấy danh sách Farmer tương ứng và tạo dictionary tên
                var farmers = await _unitOfWork.FarmerRepository.GetAllAsync(
                    f => updatedByIds.Contains(f.FarmerId),
                    include: q => q.Include(f => f.User)
                );

                var farmerNameDict = farmers.ToDictionary(f => f.FarmerId, f => f.User?.Name ?? "N/A");

                var progresses = new List<ProcessingProgressWithStageDto>();

                foreach (var p in batch.ProcessingBatchProgresses
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.StepIndex))
                {
                    var mediaFiles = await _unitOfWork.MediaFileRepository.GetAllAsync(
                        m => !m.IsDeleted &&
                             m.RelatedEntity == "ProcessingProgress" &&
                             m.RelatedId == p.ProgressId,
                        orderBy: q => q.OrderByDescending(m => m.UploadedAt)
                    );

                    var mediaDtos = mediaFiles.Select(m => new MediaFileResponse
                    {
                        MediaId = m.MediaId,
                        MediaUrl = m.MediaUrl,
                        MediaType = m.MediaType,
                        Caption = m.Caption,
                        UploadedAt = m.UploadedAt
                    }).ToList();

                    var parameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                        x => x.ProgressId == p.ProgressId && !x.IsDeleted
                    );

                    var parameterDtos = parameters.Select(param => new ProcessingParameterViewAllDto
                    {
                        ParameterId = param.ParameterId,
                        ParameterName = param.ParameterName,
                        ParameterValue = param.ParameterValue,
                        Unit = param.Unit,
                        RecordedAt = param.RecordedAt
                    }).ToList();

                    var wastes = p.ProcessingBatchWastes?.Select(w => new ProcessingWasteViewAllDto
                    {
                        WasteId = w.WasteId,
                        WasteCode = w.WasteCode,
                        WasteType = w.WasteType,
                        Quantity = w.Quantity ?? 0,
                        Unit = w.Unit,
                        CreatedAt = w.CreatedAt
                    }).ToList() ?? new();

                    progresses.Add(new ProcessingProgressWithStageDto
                    {
                        ProgressId = p.ProgressId,
                        StepIndex = p.StepIndex,
                        ProgressDate = p.ProgressDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        OutputQuantity = p.OutputQuantity ?? 0,

                        // ✅ UpdatedByName dùng từ dictionary
                        UpdatedByName = p.UpdatedBy != null && farmerNameDict.ContainsKey(p.UpdatedBy)
                            ? farmerNameDict[p.UpdatedBy]
                            : "N/A",

                        StageId = p.StageId.ToString(),
                        StageName = p.Stage?.StageName ?? "N/A",
                        StageDescription = p.StageDescription,

                        Parameters = parameterDtos,
                        Wastes = wastes,
                        MediaFiles = mediaDtos
                    });
                }

                var resultDto = batch.MapToFullDetailDto(
                    batch.Farmer?.User?.Name ?? "N/A",
                    batch.CoffeeType?.TypeName ?? "Unknown",
                    batch.CropSeason?.SeasonName ?? "Unknown",
                    batch.Method?.Name ?? "Unknown",
                    progresses
                );

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết thành công", resultDto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GetFarmersWithBatchesForBusinessManagerAsync(Guid managerUserId)
        {
            try
            {
                // Kiểm tra Business Manager có tồn tại không
                var manager = await _unitOfWork.BusinessManagerRepository
                    .GetByIdAsync(m => m.UserId == managerUserId && !m.IsDeleted);

                if (manager == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Business Manager tương ứng.");
                }

                // Lấy tất cả FarmingCommitment được approved bởi manager này (chỉ đọc dữ liệu)
                var commitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                    predicate: fc => !fc.IsDeleted && fc.ApprovedBy == manager.ManagerId,
                    include: q => q.Include(fc => fc.CropSeasons)
                );

                // Debug: Log số lượng commitment và farmer
                Console.WriteLine($"🔍 Business Manager {manager.ManagerId} có {commitments.Count} commitment được approved");
                foreach (var commitment in commitments)
                {
                    Console.WriteLine($"📋 Commitment {commitment.CommitmentId}: Farmer {commitment.FarmerId}");
                }

                if (!commitments.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        "Không có cam kết nào được approved bởi Business Manager này.");
                }

                // Lấy tất cả CropSeasonId từ các commitment
                var cropSeasonIds = commitments
                    .SelectMany(fc => fc.CropSeasons)
                    .Where(cs => !cs.IsDeleted)
                    .Select(cs => cs.CropSeasonId)
                    .ToList();

                // Debug: Log crop seasons
                Console.WriteLine($"🌾 Có {cropSeasonIds.Count} crop seasons từ commitments:");
                foreach (var cropSeasonId in cropSeasonIds)
                {
                    Console.WriteLine($"   - CropSeason: {cropSeasonId}");
                }

                if (!cropSeasonIds.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        "Không có mùa vụ nào từ các cam kết.");
                }

                // Lấy tất cả ProcessingBatch trong các crop season này
                var batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: pb => !pb.IsDeleted && cropSeasonIds.Contains(pb.CropSeasonId),
                    include: query => query
                        .Include(x => x.Farmer).ThenInclude(f => f.User),
                    asNoTracking: true
                );

                // Debug: Log batches
                Console.WriteLine($"📦 Có {batches.Count} batches trong các crop seasons:");
                foreach (var batch in batches)
                {
                    Console.WriteLine($"   - Batch {batch.BatchId}: Farmer {batch.FarmerId} ({batch.Farmer?.User?.Name}) - CropSeason {batch.CropSeasonId}");
                }

                if (!batches.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        "Chưa có lô sơ chế nào trong các mùa vụ đã cam kết.");
                }

                // Lấy danh sách unique farmers có batches (chỉ từ các commitment được approved)
                var farmers = batches
                    .GroupBy(b => b.FarmerId)
                    .Select(g => new
                    {
                        FarmerId = g.Key,
                        FarmerName = g.First().Farmer.User?.Name ?? g.First().Farmer.FarmerCode,
                        BatchCount = g.Count()
                    })
                    .OrderBy(f => f.FarmerName)
                    .ToList();

                return new ServiceResult(Const.SUCCESS_READ_CODE, 
                    $"Đã tìm thấy {farmers.Count} nông dân có lô sơ chế", 
                    farmers);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GetBatchesByFarmerForBusinessManagerAsync(Guid managerUserId, Guid farmerId)
        {
            try
            {
                // Kiểm tra Business Manager có tồn tại không
                var manager = await _unitOfWork.BusinessManagerRepository
                    .GetByIdAsync(m => m.UserId == managerUserId && !m.IsDeleted);

                if (manager == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Business Manager tương ứng.");
                }

                // Kiểm tra Farmer có tồn tại không
                var farmer = await _unitOfWork.FarmerRepository
                    .GetByIdAsync(f => f.FarmerId == farmerId && !f.IsDeleted);

                if (farmer == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Farmer tương ứng.");
                }

                // Lấy tất cả FarmingCommitment của farmer này được approved bởi manager này (chỉ đọc dữ liệu)
                var commitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                    predicate: fc => !fc.IsDeleted && 
                                   fc.FarmerId == farmerId && 
                                   fc.ApprovedBy == manager.ManagerId,
                    include: q => q.Include(fc => fc.CropSeasons)
                );

                if (!commitments.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        $"Không có cam kết nào của farmer {farmer.User?.Name ?? farmer.FarmerCode} được approved bởi Business Manager này.");
                }

                // Lấy tất cả CropSeasonId từ các commitment
                var cropSeasonIds = commitments
                    .SelectMany(fc => fc.CropSeasons)
                    .Where(cs => !cs.IsDeleted)
                    .Select(cs => cs.CropSeasonId)
                    .ToList();

                if (!cropSeasonIds.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        "Không có mùa vụ nào từ các cam kết của farmer này.");
                }

                // Lấy tất cả ProcessingBatch của farmer trong các crop season này
                var batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                    predicate: pb => !pb.IsDeleted && 
                                   pb.FarmerId == farmerId && 
                                   cropSeasonIds.Contains(pb.CropSeasonId),
                    include: query => query
                        .Include(x => x.Method)
                        .Include(x => x.CropSeason)
                        .Include(x => x.Farmer).ThenInclude(f => f.User)
                        .Include(x => x.CoffeeType),
                    orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                    asNoTracking: true
                );

                if (!batches.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, 
                        $"Farmer {farmer.User?.Name ?? farmer.FarmerCode} chưa có lô sơ chế nào trong các mùa vụ đã cam kết.");
                }

                var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, 
                    $"Đã tìm thấy {dtoList.Count} lô sơ chế của farmer {farmer.User?.Name ?? farmer.FarmerCode}", 
                    dtoList);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi: {ex.Message}");
            }
        }

    }
}