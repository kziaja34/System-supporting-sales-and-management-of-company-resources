using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IStockAlertService : IGenericService<StockAlert>
{
    Task<IEnumerable<StockAlert>> GetUnseenAsync();
    Task MarkAsSeenAsync(int alertId);
}