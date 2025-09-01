using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IBranchService : IGenericService<Branch>
{
    Task UpdateBranchAsync(int branchId, Branch branch, CancellationToken ct = default);
}