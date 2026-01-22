FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY InvoiceApi.sln ./
COPY ./InvoiceExtraction/InvoiceExtraction.csproj ./InvoiceExtraction/InvoiceExtraction.csproj
COPY ./InvoiceApi/InvoiceApi.csproj ./InvoiceApi/InvoiceApi.csproj
RUN dotnet restore

COPY . .

RUN dotnet publish ./InvoiceApi/InvoiceApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    ocrmypdf \
    tesseract-ocr \
    tesseract-ocr-pol \
    ghostscript \
    qpdf \
    unpaper \
    pngquant && \
    jbig2 && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN useradd -m appuser
RUN mkdir /app/tmp && chown appuser:appuser /app/tmp
ENV TMPDIR=/app/tmp
USER appuser

ENTRYPOINT ["dotnet", "InvoiceApi.dll"]
