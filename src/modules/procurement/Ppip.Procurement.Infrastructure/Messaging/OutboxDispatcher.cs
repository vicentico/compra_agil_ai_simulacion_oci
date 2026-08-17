using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ppip.BuildingBlocks.Messaging;
using RabbitMQ.Client;

namespace Ppip.Procurement.Infrastructure.Messaging;

/// <summary>
/// Drena el outbox (ADR-003) hacia RabbitMQ: publica lo pendiente y lo marca
/// publicado. Si RabbitMQ está caído (F10), el outbox sigue acumulando sin
/// publicar — nada se pierde — y este servicio reconecta con backoff fijo
/// hasta que vuelva.
/// </summary>
public sealed class OutboxDispatcher(
    IOutboxStore outbox,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var factory = new ConnectionFactory { HostName = opts.Host, UserName = opts.Username, Password = opts.Password };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await channel.ExchangeDeclareAsync(opts.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

                await DrainLoopAsync(channel, opts.Exchange, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxDispatcher: no se pudo conectar/publicar en RabbitMQ, reintenta en {Delay}.", ReconnectDelay);
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task DrainLoopAsync(IChannel channel, string exchange, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pending = await outbox.GetPendingAsync(BatchSize, stoppingToken);
            if (pending.Count == 0)
            {
                await Task.Delay(PollDelay, stoppingToken);
                continue;
            }

            foreach (var message in pending)
            {
                var body = Encoding.UTF8.GetBytes(message.PayloadJson);
                var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

                await channel.BasicPublishAsync(exchange, message.RoutingKey, mandatory: false, properties, body, stoppingToken);
                await outbox.MarkPublishedAsync(message.Id, DateTimeOffset.UtcNow, stoppingToken);
            }
        }
    }
}
