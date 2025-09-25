using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _svc;

    public WarehouseController(IWarehouseService svc) => _svc = svc;

    [HttpPost("orders/{orderId}/reserve")]
    public async Task<ActionResult<ReserveResult>> Reserve(int orderId, [FromBody] int? preferredBranchId)
        => Ok(await _svc.ReserveForOrderAsync(orderId, preferredBranchId));

    [HttpPost("orders/{orderId}/fulfill")]
    public async Task<IActionResult> Fulfill(int orderId)
    { await _svc.FulfillReservationsAsync(orderId); return NoContent(); }

    [HttpPost("orders/{orderId}/release")]
    public async Task<IActionResult> Release(int orderId)
    { await _svc.ReleaseReservationsForOrderAsync(orderId); return NoContent(); }
}
