using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services;
using System.Threading.Tasks;
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
                var pdfDocument = await invoiceService.GetInvoice(orderId);
                
                var stream = new MemoryStream();
                pdfDocument.Save(stream);
                
                Response.ContentType = "application/pdf";
                Response.Headers.Append("content-length", stream.Length.ToString());
                byte[] bytes = stream.ToArray();
                stream.Close();
                
                return File(bytes, "application/pdf", "Invoice.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}