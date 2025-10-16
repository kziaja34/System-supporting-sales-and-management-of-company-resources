using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services;
using Microsoft.AspNetCore.Authorization;

namespace SSSMCR.ApiService.Controller
{
    [Route("api/invoices")]
    [ApiController]
    [Authorize]
    public class InvoiceController(IInvoiceService invoiceService) : ControllerBase
    {
        [HttpGet("generate/{orderId}")]
        [Authorize(Roles = "Administrator, Seller, Manager")]
        public async Task<IActionResult> GenerateInvoice(int orderId)
        {
            try
            {
                var pdfBytes = await invoiceService.GetInvoiceBytesAsync(orderId);
                return File(pdfBytes, "application/pdf", $"Invoice_{orderId}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}