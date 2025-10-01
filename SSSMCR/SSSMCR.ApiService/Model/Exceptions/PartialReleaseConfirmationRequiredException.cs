namespace SSSMCR.ApiService.Model.Exceptions;

public class PartialReleaseConfirmationRequiredException(int orderId) : Exception(
    $"Order {orderId} is partially fulfilled. Confirmation is required to release remaining reservations.")
{
    public int OrderId { get; } = orderId;
}