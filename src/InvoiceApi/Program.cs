using InvoiceExtraction;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddControllers();
services.AddSingleton<IInvoiceExtractor, InvoiceExtractor>();

services.Configure<InvoiceExtractionOptions>(
    builder.Configuration.GetSection(InvoiceExtractionOptions.InvoiceExtraction));

var app = builder.Build();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
