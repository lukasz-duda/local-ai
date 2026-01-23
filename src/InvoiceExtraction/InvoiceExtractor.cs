using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;
using Microsoft.Extensions.Options;

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
    byte[] pdfContent = await ReadBytesAsync(pdfStream);
    return await ExtractInvoiceAsync(pdfContent);
  }

  private static async Task<byte[]> ReadBytesAsync(Stream stream)
  {
    using var memoryStream = new MemoryStream();
    await stream.CopyToAsync(memoryStream);
    return memoryStream.ToArray();
  }

  public async Task<Invoice?> ExtractInvoiceAsync(byte[] pdfContent)
  {
    string pdfText = await ExtractPdfText(pdfContent);
    var invoice = await ExtractInvoiceJson(pdfText);
    return invoice;
  }

  private static async Task<string> ExtractPdfText(byte[] pdfContent)
  {
    string inputPdf = Path.GetTempFileName();
    string outputPdf = Path.GetTempFileName();
    string outputText = Path.GetTempFileName();

    File.WriteAllBytes(inputPdf, pdfContent);

    var startInfo = new ProcessStartInfo
    {
      FileName = "ocrmypdf",
      Arguments = $"-l pol --output-type pdf --optimize 0 --sidecar {outputText} {inputPdf} {outputPdf}",
      UseShellExecute = false
    };

    Process.Start(startInfo)?.WaitForExit();

    string pdfText = await File.ReadAllTextAsync(outputText);

    Console.WriteLine("Extracted PDF Text:");
    Console.WriteLine(pdfText);

    File.Delete(inputPdf);
    File.Delete(outputPdf);
    File.Delete(outputText);
    return pdfText;
  }

  public async Task<Invoice?> ExtractInvoiceJson(string invoiceText)
  {
    using var http = new HttpClient
    {
      BaseAddress = new Uri(_options.OllamaUri)
    };

    http.Timeout = TimeSpan.FromMinutes(10);

    var schema = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(Invoice));
    var schemaString = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });

    var systemPrompt = $"""
You are a data extractor working on OCR text from a Polish PDF invoice.
Extract the required fields and return them as JSON using the following schema:
{schemaString}
Do not invent data. If a field is missing, set its value to null.
""";

    var requestBody = new
    {
      model = "SpeakLeash/bielik-11b-v3.0-instruct:Q4_K_M",
      messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = invoiceText }
        },
      format = schema,
      stream = false,
      temperature = 0
    };

    var serializedRequest = JsonSerializer.Serialize(requestBody);
    var prettyRequest = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine("Sending request to Ollama...");
    Console.WriteLine(prettyRequest);

    var response = await http.PostAsync(
        "/api/chat",
        new StringContent(
            serializedRequest,
            Encoding.UTF8,
            "application/json"
        )
    );

    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadAsStringAsync();

    var parsed = JsonDocument.Parse(responseJson);
    var prettyResponse = JsonSerializer.Serialize(parsed.RootElement, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine("Received response from Ollama:");
    Console.WriteLine(prettyResponse);

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