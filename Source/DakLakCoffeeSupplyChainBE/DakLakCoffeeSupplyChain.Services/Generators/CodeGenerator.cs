using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public class CodeGenerator(IUnitOfWork unitOfWork) : ICodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private static int CurrentYear => DateTime.UtcNow.Year;

        public async Task<string> GenerateUserCodeAsync()
        {
            // Đếm số user tạo trong năm
            var count = await _unitOfWork.UserAccountRepository
                .CountUsersRegisteredInYearAsync(CurrentYear);

            return $"USR-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateManagerCodeAsync()
        {
            // Đếm số manager tạo trong năm
            var count = await _unitOfWork.BusinessManagerRepository
                .CountBusinessManagersRegisteredInYearAsync(CurrentYear);

            return $"BM-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateBuyerCodeAsync(Guid managerId)
        {
            // Đếm số buyer mà manager này đã tạo trong năm
            var count = await _unitOfWork.BusinessBuyerRepository
                .CountBuyersCreatedByManagerInYearAsync(managerId, CurrentYear);

            // Lấy ManagerCode để dùng làm tiền tố
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.ManagerId == managerId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            var managerCode = manager?.ManagerCode;

            if (string.IsNullOrWhiteSpace(managerCode))
                managerCode = "BM-UNKNOWN"; // fallback

            return $"{managerCode}-BUY-{CurrentYear}-{(count + 1):D3}";
        }

        public async Task<string> GenerateContractCodeAsync()
        {
            // Đếm số hợp đồng tạo trong năm hiện tại
            var count = await _unitOfWork.ContractRepository
                .CountContractsInYearAsync(CurrentYear);

            // Trả về mã có dạng: CTR-2025-0032
            return $"CTR-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateContractItemCodeAsync(Guid contractId)
        {
            // Lấy contract để có ContractCode
            var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                predicate: c => 
                   c.ContractId == contractId && 
                   !c.IsDeleted,
                asNoTracking: true
            );

            if (contract == null || 
                string.IsNullOrWhiteSpace(contract.ContractCode))
            {
                return $"CTI-UNKNOWN-{CurrentYear}-{Guid.NewGuid().ToString()[..3]}"; // fallback tránh null
            }

            var contractCode = contract.ContractCode
                .Replace(" ", "").ToUpper();

            // Đếm số item trong hợp đồng này
            var count = await _unitOfWork.ContractItemRepository
                .CountByContractIdAsync(contractId);

            // Format: CTI-001-HDX2025
            return $"CTI-{(count + 1):D3}-{contractCode}";
        }

        public async Task<string> GenerateDeliveryBatchCodeAsync()
        {
            // Đếm số lô giao hàng đã tạo trong năm hiện tại
            var count = await _unitOfWork.ContractDeliveryBatchRepository
                .CountByYearAsync(CurrentYear);

            // Trả về mã có dạng: DELB-2025-0034
            return $"DELB-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateContractDeliveryItemCodeAsync(Guid deliveryBatchId)
        {
            // Lấy contractDeliveryBatch để có DeliveryBatchCode
            var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                predicate: cdb => 
                   cdb.DeliveryBatchId == deliveryBatchId &&
                   !cdb.IsDeleted,
                asNoTracking: true
            );

            if (contractDeliveryBatch == null || 
                string.IsNullOrWhiteSpace(contractDeliveryBatch.DeliveryBatchCode))
            {
                return $"DLI-UNKNOWN-{CurrentYear}-{Guid.NewGuid().ToString()[..3]}"; // fallback tránh null
            }

            var deliveryBatchCode = contractDeliveryBatch.DeliveryBatchCode
                .Replace(" ", "").ToUpper();

            // Đếm số item trong danh mục giao của hợp đồng này
            var count = await _unitOfWork.ContractDeliveryItemRepository
                .CountByDeliveryBatchIdAsync(deliveryBatchId);

            // Format: DLI-001-HDX2025
            return $"DLI-{(count + 1):D3}-{deliveryBatchCode}";
        }

        public async Task<string> GenerateCropSeasonCodeAsync(int year)
        {
            string prefix = $"SEASON-{year}-";

            var codes = await _unitOfWork.CropSeasonRepository
                .GetQuery()
                .Where(x => x.CropSeasonCode.StartsWith(prefix))
                .Select(x => x.CropSeasonCode)
                .ToListAsync();

            int maxNumber = codes
                .Select(code =>
                {
                    var suffix = code.Substring(prefix.Length);
                    return int.TryParse(suffix, out int number) ? number : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix}{(maxNumber + 1):D4}";
        }

        public async Task<string> GenerateProcurementPlanCodeAsync()
        {
            // Đếm số procurement plan tạo trong năm
            var count = await _unitOfWork.ProcurementPlanRepository
                .CountProcurementPlansInYearAsync(CurrentYear);

            return $"PLAN-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateProcurementPlanDetailsCodeAsync()
        {
            // Đếm số procurement plan detail tạo trong năm
            //var count = await _unitOfWork.ProcurementPlanDetailsRepository
                //.CountProcurementPlanDetailsInYearAsync(CurrentYear);

            string prefix = $"PLD-{CurrentYear}-";

            var latestCode = await _unitOfWork.ProcurementPlanDetailsRepository.GetByPredicateAsync(
                predicate: x => x.PlanDetailCode.StartsWith(prefix),
                selector: x => x.PlanDetailCode,
                orderBy: x => x.OrderByDescending(x => x.PlanDetailCode),
                asNoTracking: true
                ) ?? throw new InvalidOperationException("Không tìm thấy mã PlanDetailCode nào phù hợp với prefix.");

            var count = GeneratedCodeHelpler
                .GetGeneratedCodeLastNumber(latestCode);

            return $"PLD-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateCoffeeTypeCodeAsync()
        {
            var count = await _unitOfWork.CoffeeTypeRepository
                .CountCoffeeTypeInYearAsync(CurrentYear);

            return $"CFT-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateFarmerCodeAsync()
        {
            var count = await _unitOfWork.FarmerRepository
                .CountFarmerInYearAsync(CurrentYear);

            return $"FRM-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateGeneralFarmerReportCodeAsync()
        {
            const int maxRetry = 5;

            for (int attempt = 0; attempt < maxRetry; attempt++)
            {
                // Lấy mã lớn nhất hiện tại (ví dụ: "RPT-2025-0012")
                var maxCode = await _unitOfWork.GeneralFarmerReportRepository
                    .GetMaxReportCodeForYearAsync(CurrentYear);

                int nextNumber = 1;

                if (!string.IsNullOrWhiteSpace(maxCode))
                {
                    var parts = maxCode.Split('-');

                    if (parts.Length == 3 && int.TryParse(parts[2], out int currentNumber))
                    {
                        nextNumber = currentNumber + 1;
                    }
                }

                var newCode = $"RPT-{CurrentYear}-{nextNumber:D4}";

                // Kiểm tra trùng (cẩn tắc vô ưu)
                var exists = await _unitOfWork.GeneralFarmerReportRepository
                    .AnyAsync(r => r.ReportCode == newCode);

                if (!exists)
                    return newCode;
            }

            throw new InvalidOperationException("Không thể tạo mã báo cáo duy nhất sau nhiều lần thử.");
        }

        public async Task<string> GenerateCultivationRegistrationCodeAsync()
        {
            var count = await _unitOfWork.CultivationRegistrationRepository
                .CountCultivationRegistrationsInYearAsync(CurrentYear);

            return $"REG-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateStaffCodeAsync()
        {
            var count = await _unitOfWork.BusinessStaffRepository
                .CountStaffCreatedInYearAsync(CurrentYear);

            return $"STAFF-{CurrentYear}-{(count + 1):D4}"; 
        }

        public async Task<string> GenerateInboundRequestCodeAsync()
        {
            var count = await _unitOfWork.WarehouseInboundRequests
                .CountInboundRequestsInYearAsync(DateTime.UtcNow.Year);

            return $"IR-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }

        public async Task<string> GenerateWarehouseCodeAsync()
        {
            const int maxRetry = 5;

            for (int attempt = 0; attempt < maxRetry; attempt++)
            {
                // Đếm số warehouse đã tạo trong năm (chỉ những cái chưa bị xóa thật)
                var count = await _unitOfWork.Warehouses
                    .GetAllQueryable()
                    .CountAsync(w => w.CreatedAt.Year == CurrentYear && !w.IsDeleted);

                var newCode = $"WH-{CurrentYear}-{(count + 1):D4}";

                // Kiểm tra xem mã này đã tồn tại chưa (chỉ check warehouse chưa bị xóa thật)
                var exists = await _unitOfWork.Warehouses
                    .AnyAsync(w => w.WarehouseCode == newCode && !w.IsDeleted);

                if (!exists)
                    return newCode;
            }

            throw new InvalidOperationException("Không thể tạo mã kho duy nhất sau nhiều lần thử.");
        }

        public async Task<string> GenerateProcessingSystemBatchCodeAsync(int year)
        {
            // Lấy tất cả SystemBatchCode có cùng năm
            var prefix = $"BATCH-{year}-";

            var latestCode = await _unitOfWork.ProcessingBatchRepository
                .GetQueryable()
                .Where(x => x.SystemBatchCode.StartsWith(prefix))
                .OrderByDescending(x => x.SystemBatchCode)
                .Select(x => x.SystemBatchCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(latestCode))
            {
                var suffix = latestCode.Substring(prefix.Length); // lấy "0005"

                if (int.TryParse(suffix, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<string> GenerateProcessingWasteCodeAsync()
        {
            var count = await _unitOfWork.ProcessingWasteRepository
                .CountCreatedInYearAsync(CurrentYear);

            return $"WASTE-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateWarehouseReceiptCodeAsync()
        {
            string prefix = $"WR-{CurrentYear}-";

            var latestCode = await _unitOfWork.WarehouseReceipts
                .GetQuery()
                .Where(x => x.ReceiptCode.StartsWith(prefix))
                .OrderByDescending(x => x.ReceiptCode)
                .Select(x => x.ReceiptCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(latestCode))
            {
                var suffix = latestCode.Substring(prefix.Length);

                if (int.TryParse(suffix, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<string> GenerateInventoryCodeAsync()
        {
            var count = await _unitOfWork.Inventories
                .CountCreatedInYearAsync(DateTime.UtcNow.Year);

            return $"INV-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }

        public async Task<string> GenerateProductCodeAsync(Guid managerId)
        {
            // Đếm số sản phẩm được tạo bởi manager này trong năm hiện tại
            var count = await _unitOfWork.ProductRepository
                .CountByManagerIdInYearAsync(managerId, CurrentYear);

            // Lấy ManagerCode để làm tiền tố
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.ManagerId == managerId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            var managerCode = manager?.ManagerCode?
                .Replace(" ", "").ToUpper() ?? "BM-UNKNOWN";

            // Format: PROD-001-BM-2025-0001
            return $"PROD-{(count + 1):D3}-{managerCode}";
        }

        public async Task<string> GenerateOrderCodeAsync()
        {
            // Đếm số đơn hàng đã được tạo trong năm hiện tại
            var count = await _unitOfWork.OrderRepository
                .CountOrdersInYearAsync(CurrentYear);

            // Trả về mã có dạng: ORD-2025-0001
            return $"ORD-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateShipmentCodeAsync()
        {
            // Đếm số shipment đã được tạo trong năm hiện tại
            var count = await _unitOfWork.ShipmentRepository
                .CountShipmentsInYearAsync(CurrentYear);

            // Trả về mã có dạng: SHIP-2025-0001
            return $"SHIP-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateOutboundRequestCodeAsync()
        {
            var count = await _unitOfWork.WarehouseOutboundRequests
                .CountOutboundRequestsInYearAsync(CurrentYear);

            return $"WOR-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateNotificationCodeAsync()
        {
            string prefix = $"NT-{CurrentYear}-";

            var latestCode = await _unitOfWork.SystemNotificationRepository
                .GetQuery()
                .Where(x => x.NotificationCode.StartsWith(prefix))
                .OrderByDescending(x => x.NotificationCode)
                .Select(x => x.NotificationCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(latestCode))
            {
                var suffix = latestCode.Substring(prefix.Length);

                if (int.TryParse(suffix, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<string> GenerateFarmingCommitmentCodeAsync()
        {
            var count = await _unitOfWork.FarmingCommitmentRepository
                .CountFarmingCommitmentsInYearAsync(CurrentYear);

            return $"FC-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateFarmingCommitmenstDetailCodeAsync()
        {
            string prefix = $"FCD-{CurrentYear}-";

            var latestCode = await _unitOfWork.FarmingCommitmentsDetailRepository.GetByPredicateAsync(
                predicate: x => x.CommitmentDetailCode.StartsWith(prefix),
                selector: x => x.CommitmentDetailCode,
                orderBy: x => x.OrderByDescending(x => x.CommitmentDetailCode),
                asNoTracking: true
                ) ?? throw new InvalidOperationException("Không tìm thấy mã CommitmentDetailCode nào phù hợp với prefix.");

            var count = GeneratedCodeHelpler
                .GetGeneratedCodeLastNumber(latestCode);

            return $"FCD-{CurrentYear}-{(count + 1):D4}";
        }
        
        public async Task<string> GenerateProcessingWasteDisposalCodeAsync()
        {
            var prefix = $"DISP-{CurrentYear}-";

            var latestCode = await _unitOfWork.ProcessingWasteDisposalRepository
                .GetAllQueryable()
                .Where(x => x.DisposalCode.StartsWith(prefix)) 
                .OrderByDescending(x => x.DisposalCode)
                .Select(x => x.DisposalCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(latestCode))
            {
                var suffix = latestCode.Substring(prefix.Length);

                if (int.TryParse(suffix, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
        public async Task<string> GenerateEvaluationCodeAsync(int year)
        {
            var prefix = $"EVAL-{year}-";
            var lastCode = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByPredicateAsync(
                predicate: x => x.EvaluationCode.StartsWith(prefix),
                selector: x => x.EvaluationCode,
                include: null,
                orderBy: q => q.OrderByDescending(e => e.EvaluationCode),
                asNoTracking: true
            );
            var next = 1;
            if (!string.IsNullOrWhiteSpace(lastCode))
            {
                var seg = lastCode.Split('-').Last();
                if (int.TryParse(seg, out var n)) next = n + 1;
            }
            return $"{prefix}{next:0000}";
        }

        public async Task<string> GenerateExpertCodeAsync()
        {
            // Đếm số chuyên gia tạo trong năm
            var count = await _unitOfWork.AgriculturalExpertRepository
                .CountExpertsRegisteredInYearAsync(CurrentYear);

            return $"EXP-{CurrentYear}-{(count + 1):D4}";
        }
    }
}
