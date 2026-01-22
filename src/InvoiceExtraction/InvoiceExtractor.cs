using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace InvoiceExtraction;

public class InvoiceExtractor : IInvoiceExtractor
{
  private readonly InvoiceExtractionOptions _options;

  public InvoiceExtractor(IOptions<InvoiceExtractionOptions> options)
  {
    _options = options.Value;
  }

  public async Task<Invoice?> ExtractAsync(Stream pdfStream)
  {
    using var memoryStream = new MemoryStream();
    await pdfStream.CopyToAsync(memoryStream);
    byte[] pdfContent = memoryStream.ToArray();

    return await ExtractInvoiceAsync(pdfContent);
  }

  public async Task<Invoice?> ExtractInvoiceAsync(byte[] pdfContent)
  {
    byte[] textPdfContent = OcrPdf(pdfContent);
    var pdfText = ExtractPdfText(textPdfContent);
    var invoice = await ExtractInvoiceJson(pdfText);
    return invoice;
  }

  private static byte[] OcrPdf(byte[] pdfContent)
  {
    string temporaryInputPath = Path.GetTempFileName();
    File.WriteAllBytes(temporaryInputPath, pdfContent);
    string temporaryOutputPath = Path.GetTempFileName();
    var process = Process.Start("ocrmypdf", $"-l pol --skip-text --output-type pdf {temporaryInputPath} {temporaryOutputPath}");
    process.WaitForExit();
    byte[] textPdfContent = File.ReadAllBytes(temporaryOutputPath);
    File.Delete(temporaryInputPath);
    File.Delete(temporaryOutputPath);
    return textPdfContent;
  }

  public static string ExtractPdfText(byte[] content)
  {
    var sb = new StringBuilder();

    using var pdf = PdfDocument.Open(content);

    foreach (var page in pdf.GetPages())
    {
      sb.AppendLine(page.Text);
    }

    return sb.ToString();
  }

  public async Task<Invoice?> ExtractInvoiceJson(string invoiceText)
  {
    using var http = new HttpClient
    {
      BaseAddress = new Uri(_options.OllamaUri)
    };

    http.Timeout = TimeSpan.FromMinutes(5);

    var systemPrompt = """
You are a deterministic JSON extraction engine.
Return valid JSON only.
Do not include explanations or markdown.

Return JSON exactly matching this schema:

{
  "invoiceNumber": string | null,
  "issueDate": string | null,
  "saleDate": string | null,
  "seller": {
    "name": string | null,
    "address": string | null,
    "nip": string | null
  } | null,
  "buyer": {
    "name": string | null,
    "address": string | null,
    "nip": string | null
  } | null,
  "items": [
    {
      "name": string | null,
      "quantity": number,
      "unit": string | null,
      "netPrice": number | null,
      "netValue": number | null,
      "vatRate": string | null,
      "vatValue": number | null,
      "grossValue": number | null
    }
  ] | null,
  "totals": {
    "netTotal": number | null,
    "vatTotal": number | null,
    "grossTotal": number | null
  } | null
}

Use null when a value is missing.
Do not add extra fields.
""";

    var requestBody = new
    {
      model = "SpeakLeash/bielik-11b-v3.0-instruct:Q4_K_M",
      messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = invoiceText }
        },
      format = "json",
      stream = false
    };

    var response = await http.PostAsync(
        "/api/chat",
        new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        )
    );

    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadAsStringAsync();

    using var doc = JsonDocument.Parse(responseJson);

    var content = doc.RootElement
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

    if (string.IsNullOrWhiteSpace(content))
      return null;

    return JsonSerializer.Deserialize<Invoice>(
        content,
        new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        }
    );
  }
}