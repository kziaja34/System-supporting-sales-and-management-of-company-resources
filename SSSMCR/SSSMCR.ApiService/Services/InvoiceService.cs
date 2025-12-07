using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp;
using PdfSharp.Fonts;
using PdfSharp.Snippets.Font;
using SSSMCR.ApiService.Model;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;

namespace SSSMCR.ApiService.Services
{
    public interface IInvoiceService
    {
        Task<byte[]> GetInvoiceBytesAsync(int orderId);
        Task<Invoice> SaveInvoiceAsync(int orderId, byte[] pdfBytes);
    }

    public class InvoiceService(AppDbContext context, IOrderService orderService) : IInvoiceService
    {
        private readonly Company _company = context.Companies.FirstOrDefault() ?? new();
        private static readonly CultureInfo Pl = new("pl-PL");
        private const decimal VatRate = 0.23m;

        public async Task<byte[]> GetInvoiceBytesAsync(int orderId)
        {
            var existing = await context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
            if (existing != null)
                return existing.FileData;

            var document = new Document();
            await BuildDocument(document, orderId);
            var pdfRenderer = new PdfDocumentRenderer();
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();

            using var stream = new MemoryStream();
            pdfRenderer.PdfDocument.Save(stream);
            var pdfBytes = stream.ToArray();

            await SaveInvoiceAsync(orderId, pdfBytes);
            return pdfBytes;
        }
        
        public async Task<Invoice> SaveInvoiceAsync(int orderId, byte[] pdfBytes)
        {
            var existing = await context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
            if (existing != null)
                return existing;

            var invoice = new Invoice
            {
                OrderId = orderId,
                FileName = $"Invoice_{orderId}_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                FileData = pdfBytes,
                CreatedAt = DateTime.UtcNow
            };

            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            return invoice;
        }

        private async Task BuildDocument(Document document, int orderId)
        {
            var order = await orderService.GetByIdAsync(orderId);
            if (order is null) throw new InvalidOperationException("Order not found.");
            
            if (order.Status.ToString() != "Completed")
                throw new InvalidOperationException("Cannot generate invoice for order in status " + order.Status);

            if (Capabilities.Build.IsCoreBuild)
                GlobalFontSettings.FontResolver = new FailsafeFontResolver();

            document.Info.Title = "VAT Invoice";
            document.Info.Subject = "Invoice for the purchase of goods/services";
            document.Info.Author = "SSSMCR";

            DefineStyles(document);

            var section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(2.0);
            section.PageSetup.RightMargin = Unit.FromCentimeter(2.0);
            section.PageSetup.TopMargin = Unit.FromCentimeter(2.0);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(2.0);
            
            var header = section.Headers.Primary.AddParagraph();
            header.AddFormattedText("SSSMCR", TextFormat.Bold);
            header.AddLineBreak();
            header.AddText($"{_company.Address}, {_company.PostalCode} {_company.City} | NIP: {_company.TaxIdentificationNumber}");
            header.Format.Font.Size = 9;
            header.Format.Alignment = ParagraphAlignment.Left;
            
            var footer = section.Footers.Primary.AddParagraph();
            footer.AddText("Page ");
            footer.AddPageField();
            footer.AddText(" out of ");
            footer.AddNumPagesField();
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.Format.Font.Size = 9;
            
            section.AddParagraph("VAT Invoice", "Heading1");

            var meta = section.AddParagraph();
            meta.Style = "Label";
            meta.AddFormattedText($"Number: FV/{order.Id}/{DateTime.Now:yyyy}", TextFormat.Bold);
            meta.AddLineBreak();
            meta.AddText($"Date of issue: {DateTime.Now:yyyy-MM-dd}");
            meta.AddLineBreak();
            meta.AddText($"Date of sale: {order.CreatedAt:yyyy-MM-dd}");

            section.AddParagraph().AddLineBreak();
            
            var printableWidth = section.PageSetup.PageWidth - section.PageSetup.LeftMargin - section.PageSetup.RightMargin;
            
            var parties = section.AddTable();
            parties.Borders.Width = 0.5;
            parties.LeftPadding = 4;
            parties.RightPadding = 4;
            parties.Rows.LeftIndent = 0;

            var partyColWidth = Unit.FromCentimeter(8.3);
            parties.AddColumn(partyColWidth);
            parties.AddColumn(partyColWidth);

            var r0 = parties.AddRow();
            r0.Shading.Color = Colors.LightGray;
            r0.Cells[0].AddParagraph("Seller").Format.Font.Bold = true;
            r0.Cells[1].AddParagraph("Buyer").Format.Font.Bold = true;

            var r1 = parties.AddRow();
            var pSeller = r1.Cells[0].AddParagraph();
            pSeller.AddFormattedText("SSSMCR", TextFormat.Bold);
            pSeller.AddLineBreak();
            pSeller.AddText($"{_company.Address}");
            pSeller.AddLineBreak();
            pSeller.AddText($"{_company.PostalCode} {_company.City}");
            pSeller.AddLineBreak();
            pSeller.AddText($"NIP: {_company.TaxIdentificationNumber}");

            var pBuyer = r1.Cells[1].AddParagraph();
            pBuyer.AddFormattedText(order.CustomerName, TextFormat.Bold);
            pBuyer.AddLineBreak();
            pBuyer.AddText(order.ShippingAddress);

            section.AddParagraph().AddLineBreak();
            
            var items = section.AddTable();
            items.Borders.Width = 0.5;
            items.LeftPadding = 4;
            items.RightPadding = 4;
            items.Rows.LeftIndent = 0;
            items.Format.Font.Size = 9;

            items.AddColumn(Unit.FromCentimeter(6.2));
            items.AddColumn(Unit.FromCentimeter(2.0));
            items.AddColumn(Unit.FromCentimeter(2.0));
            items.AddColumn(Unit.FromCentimeter(2.4));
            items.AddColumn(Unit.FromCentimeter(1.2));
            items.AddColumn(Unit.FromCentimeter(1.6));
            items.AddColumn(Unit.FromCentimeter(2.4));

            var headerRow = items.AddRow();
            headerRow.Shading.Color = Colors.LightGray;
            headerRow.HeadingFormat = true;
            headerRow.Format.Font.Bold = true;
            headerRow.VerticalAlignment = VerticalAlignment.Center;
            headerRow.Cells[0].AddParagraph("Product/Service Name");
            headerRow.Cells[1].AddParagraph("Quantity").Format.Alignment = ParagraphAlignment.Right;
            headerRow.Cells[2].AddParagraph("Net Price").Format.Alignment = ParagraphAlignment.Right;
            headerRow.Cells[3].AddParagraph("Net Value").Format.Alignment = ParagraphAlignment.Right;
            headerRow.Cells[4].AddParagraph("VAT").Format.Alignment = ParagraphAlignment.Right;
            headerRow.Cells[5].AddParagraph("VAT Amount").Format.Alignment = ParagraphAlignment.Right;
            headerRow.Cells[6].AddParagraph("Gross Value").Format.Alignment = ParagraphAlignment.Right;

            foreach (var o in headerRow.Cells)
            {
                var c = (Cell)o!;
                c.Format.Font.Size = 9;
                c.Format.SpaceBefore = 1;
                c.Format.SpaceAfter = 1;
                c.Format.KeepTogether = true;
            }

            decimal sumNet = 0m, sumVat = 0m, sumGross = 0m;

            foreach (var item in order.Items)
            {
                var qty = item.Quantity;
                var unitNet = item.UnitPrice;
                var lineNet = Round(unitNet * qty);
                var vatAmount = Round(lineNet * VatRate);
                var lineGross = Round(lineNet + vatAmount);

                var row = items.AddRow();
                row.Cells[0].AddParagraph(item.Product.Name).Format.Alignment = ParagraphAlignment.Left;
                row.Cells[1].AddParagraph(qty.ToString()).Format.Alignment = ParagraphAlignment.Right;
                row.Cells[2].AddParagraph(Money(unitNet)).Format.Alignment = ParagraphAlignment.Right;
                row.Cells[3].AddParagraph(Money(lineNet)).Format.Alignment = ParagraphAlignment.Right;
                row.Cells[4].AddParagraph($"{VatRate:P0}").Format.Alignment = ParagraphAlignment.Right;
                row.Cells[5].AddParagraph(Money(vatAmount)).Format.Alignment = ParagraphAlignment.Right;
                row.Cells[6].AddParagraph(Money(lineGross)).Format.Alignment = ParagraphAlignment.Right;

                sumNet += lineNet;
                sumVat += vatAmount;
                sumGross += lineGross;
            }

            section.AddParagraph().AddLineBreak();
            
            var totals = section.AddTable();
            totals.Borders.Width = 0.5;
            totals.LeftPadding = 4;
            totals.RightPadding = 4;

            Unit totalsCol1 = Unit.FromCentimeter(5.5);
            Unit totalsCol2 = Unit.FromCentimeter(4.0);
            Unit totalsWidth = totalsCol1 + totalsCol2;
            
            totals.Rows.LeftIndent = (printableWidth - totalsWidth) > 0 ? (printableWidth - totalsWidth) : 0;

            totals.AddColumn(totalsCol1);
            totals.AddColumn(totalsCol2);
            totals.Format.Alignment = ParagraphAlignment.Right;

            var t1 = totals.AddRow();
            t1.Cells[0].AddParagraph("Net Total:");
            t1.Cells[1].AddParagraph(Money(sumNet)).Format.Alignment = ParagraphAlignment.Right;

            var t2 = totals.AddRow();
            t2.Cells[0].AddParagraph("VAT Total:");
            t2.Cells[1].AddParagraph(Money(sumVat)).Format.Alignment = ParagraphAlignment.Right;

            var t3 = totals.AddRow();
            t3.Shading.Color = Colors.LightGray;
            t3.Format.Font.Bold = true;
            t3.Cells[0].AddParagraph("Gross Total:");
            t3.Cells[1].AddParagraph(Money(sumGross)).Format.Alignment = ParagraphAlignment.Right;


            section.AddParagraph().AddLineBreak();
            
            var pay = section.AddParagraph();
            pay.Style = "Label";
            pay.AddText("Payment Method: bank transfer | Payment Terms: 14 days");
            pay.AddLineBreak();
            pay.AddText($"Account: {_company.BankAccountNumber}");
        }

        private void DefineStyles(Document document)
        {
            var style = document.Styles["Normal"];
            if (style != null)
            {
                style.Font.Name = "Segoe UI";
                style.Font.Size = 10;
            }

            var h1 = document.Styles["Heading1"];
            if (h1 != null)
            {
                h1.Font.Name = "Segoe UI";
                h1.Font.Size = 16;
                h1.Font.Bold = true;
                h1.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            }

            var label = document.Styles.AddStyle("Label", "Normal");
            label.Font.Size = 10;
            label.ParagraphFormat.SpaceBefore = 3;
            label.ParagraphFormat.SpaceAfter = 3;
        }

        private static string Money(decimal value) => value.ToString("C", Pl);
        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}