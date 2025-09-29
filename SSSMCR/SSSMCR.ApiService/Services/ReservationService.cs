using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;

namespace SSSMCR.ApiService.Services;

public interface IReservationService : IGenericService<StockReservation>
{
    public Task<List<StockReservation>> GetReservations(int? branchId);
}

public class ReservationService(AppDbContext context) : GenericService<StockReservation>(context), IReservationService
{
    public Task<List<StockReservation>> GetReservations(int? branchId)
    {
        var reservations = _dbSet
            .Include(r => r.OrderItem)
            .ThenInclude(oi => oi.Order)
            .Include(r => r.ProductStock)
            .ThenInclude(ps => ps.Branch)
            .Include(r => r.ProductStock.Product)
            .AsQueryable();

        if (reservations == null)
            throw new ReservationNotFoundException(0);
        
        if (branchId.HasValue)
        {
            reservations = reservations.Where(r => r.ProductStock.BranchId == branchId);
        }

        return reservations.ToListAsync();
    }
}