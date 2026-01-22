using InvoiceExtraction;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace InvoicesApi.Controllers;

[ApiController]
[Route("invoices")]
public class InvoiceController(IInvoiceExtractor invoiceExtractor) : ControllerBase
{
    private static List<Invoice> _invoices = new();

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadInvoice(IFormFile invoicePdfFile)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest("The request does not contain a valid form.");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var formFeature = Request.HttpContext.Features.GetRequiredFeature<IFormFeature>();
        await formFeature.ReadFormAsync(cancellationToken);

        var invoicePdfStream = invoicePdfFile.OpenReadStream();
        Invoice? invoice = await invoiceExtractor.ExtractAsync(invoicePdfStream);

        if (invoice != null)
        {
            _invoices.Add(invoice);
            return Ok();
        }
        else
        {
            return NotFound();
        }
    }

    [HttpGet]
    public OkObjectResult GetInvoices()
    {
        return Ok(_invoices.ToArray());
    }
}