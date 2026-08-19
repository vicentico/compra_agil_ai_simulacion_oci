using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Domain.Ports;
using Ppip.DocumentIntelligence.Infrastructure.Pdf;

namespace Ppip.DocumentIntelligence.Infrastructure.Ocr;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddDocumentIntelligenceProcessing(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<OcrOptions>()
            .Bind(builder.Configuration.GetSection(OcrOptions.SectionName));

        builder.Services.AddSingleton<IOcrService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OcrOptions>>().Value;
            return options.Provider.Equals("Tesseract", StringComparison.OrdinalIgnoreCase)
                ? new TesseractOcrService(sp.GetRequiredService<IOptions<OcrOptions>>())
                : new MockOcrService();
        });

        builder.Services.AddSingleton<IPdfExtractor, PdfPigExtractor>();

        return builder;
    }
}
