namespace InvoiceExtraction;

public class Invoice
{
    public string? InvoiceNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? SaleDate { get; set; }
    public Party? Seller { get; set; }
    public Party? Buyer { get; set; }
    public List<InvoiceItem>? Items { get; set; }
    public InvoiceTotals? Totals { get; set; }
}

public class Party
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Nip { get; set; }
}

public class InvoiceItem
{
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? NetPrice { get; set; }
    public decimal? NetValue { get; set; }
    public string? VatRate { get; set; }
    public decimal? VatValue { get; set; }
    public decimal? GrossValue { get; set; }
}

public class InvoiceTotals
{
    public decimal? NetTotal { get; set; }
    public decimal? VatTotal { get; set; }
    public decimal? GrossTotal { get; set; }
}
