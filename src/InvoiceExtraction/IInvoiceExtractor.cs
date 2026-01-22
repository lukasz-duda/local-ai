namespace InvoiceExtraction;

public interface IInvoiceExtractor
{
  Task<Invoice?> ExtractAsync(Stream pdfStream);
}
