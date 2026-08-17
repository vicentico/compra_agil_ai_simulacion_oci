using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Http;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="IAttachmentDownloader"/> con la misma resiliencia
    /// por defecto del proyecto (docs/14-reliability/01: 3 intentos, backoff
    /// 1s/5s/25s) más el <see cref="SsrfSafeConnect"/> callback (T3) y
    /// <c>AllowAutoRedirect = false</c> — "sin redirects fuera de allowlist"
    /// (docs/12-security/01) se resuelve de la forma más simple y segura: no
    /// seguir ningún redirect automáticamente, nunca (un enlace directo a un
    /// PDF de bases no debería necesitarlos).
    /// </summary>
    public static IHostApplicationBuilder AddDocumentDownloader(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IAttachmentDownloader, HttpAttachmentDownloader>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectCallback = SsrfSafeConnect.ConnectAsync,
                AllowAutoRedirect = false,
            })
            .AddResilienceHandler("document-download", static resilience =>
            {
                resilience.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Result?.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable
                        || IsRetryableException(args.Outcome.Exception)),
                    DelayGenerator = args => ValueTask.FromResult<TimeSpan?>(args.AttemptNumber switch
                    {
                        0 => TimeSpan.FromSeconds(1),
                        1 => TimeSpan.FromSeconds(5),
                        _ => TimeSpan.FromSeconds(25),
                    }),
                });
                resilience.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 1.0,
                    MinimumThroughput = 5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(60),
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Result?.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable
                        || IsRetryableException(args.Outcome.Exception)),
                });
                resilience.AddTimeout(TimeSpan.FromSeconds(30));
            });

        return builder;
    }

    /// <summary>
    /// Excluye del retry/circuit-breaker lo que es una decisión de política,
    /// no una falla transitoria (bloqueo SSRF, binario sobredimensionado) —
    /// incluye desenvolver <see cref="HttpRequestException"/>, porque
    /// <see cref="SocketsHttpHandler"/> envuelve ahí cualquier excepción que
    /// lance el <c>ConnectCallback</c> (hallazgo real: sin esto, Polly
    /// reintentaba 3 veces contra un destino ya bloqueado por SSRF).
    /// </summary>
    private static bool IsRetryableException(Exception? exception) => exception switch
    {
        null => false,
        OperationCanceledException => false,
        HttpRequestException httpRequestException when ConnectExceptionUnwrapper.Unwrap(httpRequestException) is not null => false,
        _ => true,
    };
}
