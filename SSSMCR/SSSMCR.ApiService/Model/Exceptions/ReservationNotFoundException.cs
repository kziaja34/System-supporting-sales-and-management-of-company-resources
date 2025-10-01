namespace SSSMCR.ApiService.Model.Exceptions;

public class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(int orderId)
        : base($"No active reservations found for order {orderId}.") { }
}