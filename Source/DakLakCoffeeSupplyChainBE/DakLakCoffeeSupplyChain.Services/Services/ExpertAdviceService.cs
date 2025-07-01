using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using Microsoft.EntityFrameworkCore;

public class ExpertAdviceService : IExpertAdviceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpertAdviceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IServiceResult> GetAllAsync()
    {
        var advices = await _unitOfWork.ExpertAdviceRepository.GetAllAsync(
            predicate: a => !a.IsDeleted,
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User),
            orderBy: q => q.OrderByDescending(a => a.CreatedAt),
            asNoTracking: true
        );

        var result = advices.Select(a => a.MapToViewAllDto()).ToList();

        return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
    }

    public async Task<IServiceResult> GetByIdAsync(Guid adviceId)
    {
        var advice = await _unitOfWork.ExpertAdviceRepository.GetByIdAsync(
            predicate: a => a.AdviceId == adviceId && !a.IsDeleted,
            include: q => q.Include(a => a.Expert).ThenInclude(e => e.User),
            asNoTracking: true
        );

        if (advice == null)
            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lời khuyên.");

        return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, advice.MapToViewDetailDto());
    }
}
