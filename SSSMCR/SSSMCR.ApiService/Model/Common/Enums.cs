namespace SSSMCR.ApiService.Model.Common;

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

public enum SupplyOrderStatus
{
    Draft,
    Ordered,
    Received,
    Cancelled
}