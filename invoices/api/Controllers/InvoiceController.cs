using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("invoices")]
public class InvoiceController(IInvoiceAnalyzer invoiceAnalyzer) : ControllerBase
{
    private static List<Invoice> _invoices = new();

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadInvoice(IFormFile invoiceFile)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest("The request does not contain a valid form.");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var formFeature = Request.HttpContext.Features.GetRequiredFeature<IFormFeature>();
        await formFeature.ReadFormAsync(cancellationToken);

        using var memoryStream = new MemoryStream();
        await invoiceFile.CopyToAsync(memoryStream);
        byte[] fileContent = memoryStream.ToArray();

        Invoice? invoice = await invoiceAnalyzer.ExtractInvoiceAsync(fileContent);

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