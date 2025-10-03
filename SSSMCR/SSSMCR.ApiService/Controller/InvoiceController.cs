using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services;
using System.Threading.Tasks;

namespace SSSMCR.ApiService.Controller
{
    [Route("api/invoices")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }
        
        [HttpGet("generate/{orderId}")]
        public async Task<IActionResult> GenerateInvoice(int orderId)
        {
            try
            {
                var pdfDocument = await _invoiceService.GetInvoice(orderId);
                
                MemoryStream stream = new MemoryStream();
                pdfDocument.Save(stream);
                
                Response.ContentType = "application/pdf";
                Response.Headers.Add("content-length", stream.Length.ToString());
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