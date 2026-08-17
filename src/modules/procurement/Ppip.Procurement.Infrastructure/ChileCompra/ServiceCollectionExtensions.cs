using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Ppip.Procurement.Infrastructure.ChileCompra;

public static class ServiceCollectionExtensions
{
    private const string HttpClientName = "ChileCompra";

    /// <summary>
    /// Registra <see cref="IChileCompraClient"/> con la resiliencia por
    /// defecto del proyecto (docs/14-reliability/01): 3 intentos, backoff
    /// 1s/5s/25s, circuit breaker 5 fallos/30s → abierto 60s. 429 (cuota
    /// diaria agotada) se excluye deliberadamente del retry automático — no
    /// es una falla transitoria, es una señal de "espera hasta mañana"
    /// (§4.3 de la guía de la API); reintentar en segundos sería inútil y
    /// desperdiciaría más cuota. 400/401/403/404 tampoco se reintentan
    /// (errores determinísticos del cliente).
    /// </summary>
    public static IHostApplicationBuilder AddChileCompraClient(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<ChileCompraOptions>()
            .Bind(builder.Configuration.GetSection(ChileCompraOptions.SectionName));

        // Cliente HTTP nombrado (no tipado): ChileCompraHttpClient necesita el
        // ticket además del HttpClient, y AddHttpClient<TClient> solo sabe
        // inyectar el HttpClient — se arma manualmente en el factory de abajo.
        builder.Services.AddHttpClient(HttpClientName, (sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<ChileCompraOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddResilienceHandler("chilecompra", static resilience =>
            {
                resilience.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Result?.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable
                        || (args.Outcome.Exception is not null && args.Outcome.Exception is not OperationCanceledException)),
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
                        || (args.Outcome.Exception is not null && args.Outcome.Exception is not OperationCanceledException)),
                });
                resilience.AddTimeout(TimeSpan.FromSeconds(30));
            });

        builder.Services.AddTransient<IChileCompraClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChileCompraOptions>>().Value;
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new ChileCompraHttpClient(httpClient, options.Ticket);
        });

        return builder;
    }
}
