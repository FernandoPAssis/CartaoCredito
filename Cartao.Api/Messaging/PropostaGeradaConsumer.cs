using System.Text;
using System.Text.Json;
using Cartao.Api.Data;
using Cartao.Api.Domain;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cartao.Api.Messaging
{
    // Fica rodando em segundo plano enquanto a API está no ar,
    // escutando a fila e emitindo os cartões das propostas aprovadas.
    public class PropostaGeradaConsumer : BackgroundService
    {
        private const string ExchangeOrigem = "proposta-gerada";              
        private const string Fila = "cartao.proposta-gerada";                  // fila deste serviço
        private const string ExchangeDeadLetter = "cartao.dead-letter";
        private const string FilaDeadLetter = "cartao.proposta-gerada.dlq";
        private const int MaximoTentativas = 3;

        private readonly IConnection _conexao;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PropostaGeradaConsumer> _logger;
        private IChannel? _canal;

        public PropostaGeradaConsumer(IConnection conexao, IServiceScopeFactory scopeFactory, ILogger<PropostaGeradaConsumer> logger)
        {
            _conexao = conexao;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _canal = await _conexao.CreateChannelAsync(cancellationToken: stoppingToken);

            // 1. Dead-letter: para onde vão as mensagens que falharam em todas as tentativas
            await _canal.ExchangeDeclareAsync(ExchangeDeadLetter, ExchangeType.Fanout, durable: true);
            await _canal.QueueDeclareAsync(FilaDeadLetter, durable: true, exclusive: false, autoDelete: false);
            await _canal.QueueBindAsync(FilaDeadLetter, ExchangeDeadLetter, string.Empty);

            // 2. Fila principal, configurada para mandar as mensagens rejeitadas para o dead-letter
            Dictionary<string, object?> argumentos = new Dictionary<string, object?>();
            argumentos.Add("x-dead-letter-exchange", ExchangeDeadLetter);

            await _canal.ExchangeDeclareAsync(ExchangeOrigem, ExchangeType.Fanout, durable: true);
            await _canal.QueueDeclareAsync(Fila, durable: true, exclusive: false, autoDelete: false, arguments: argumentos);
            await _canal.QueueBindAsync(Fila, ExchangeOrigem, string.Empty);

            // 3. Recebe uma mensagem por vez
            await _canal.BasicQosAsync(0, 1, false);

            // 4. Começa a escutar a fila (autoAck: false = nós confirmamos manualmente)
            AsyncEventingBasicConsumer consumidor = new AsyncEventingBasicConsumer(_canal);
            consumidor.ReceivedAsync += AoReceberMensagemAsync;
            await _canal.BasicConsumeAsync(Fila, autoAck: false, consumer: consumidor);

            _logger.LogInformation("Escutando a fila {Fila}", Fila);
        }

        // Chamado automaticamente a cada mensagem que chega na fila
        private async Task AoReceberMensagemAsync(object sender, BasicDeliverEventArgs mensagem)
        {
            string json = Encoding.UTF8.GetString(mensagem.Body.Span);

            // RETRY: tenta processar até 3 vezes, esperando um pouco entre as tentativas
            for (int tentativa = 1; tentativa <= MaximoTentativas; tentativa++)
            {
                try
                {
                    await ProcessarAsync(json);

                    // ACK: deu certo, o RabbitMQ pode apagar a mensagem
                    await _canal!.BasicAckAsync(mensagem.DeliveryTag, false);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao processar mensagem (tentativa {Tentativa} de {Maximo})", tentativa, MaximoTentativas);

                    if (tentativa < MaximoTentativas)
                        await Task.Delay(TimeSpan.FromSeconds(2 * tentativa));
                }
            }

            // DEAD-LETTER: falhou em todas as tentativas.
            // NACK sem devolver para a fila -> o RabbitMQ manda para a fila .dlq
            _logger.LogError("Mensagem enviada para a dead-letter queue: {Json}", json);
            await _canal!.BasicNackAsync(mensagem.DeliveryTag, false, false);
        }

        private async Task ProcessarAsync(string json)
        {
            PropostaGeradaEvento? evento = JsonSerializer.Deserialize<PropostaGeradaEvento>(json);
            if (evento == null)
                throw new InvalidOperationException("Mensagem inválida.");

            if (!evento.Aprovada)
            {
                _logger.LogInformation("Proposta do cliente {ClienteId} negada. Nenhum cartão emitido.", evento.ClienteId);
                return;
            }

            // o consumidor vive a aplicação inteira; o DbContext deve ser criado por mensagem
            using IServiceScope scope = _scopeFactory.CreateScope();
            CartaoDbContext db = scope.ServiceProvider.GetRequiredService<CartaoDbContext>();

            // IDEMPOTÊNCIA: se a mesma mensagem chegar duas vezes, não emite cartões de novo
            bool jaEmitido = await db.Cartoes.AnyAsync(c => c.PropostaId == evento.PropostaId);
            if (jaEmitido)
            {
                _logger.LogInformation("Cartões da proposta {PropostaId} já emitidos. Mensagem ignorada.", evento.PropostaId);
                return;
            }

            List<CartaoCredito> cartoes = EmissorCartao.Emitir(
                evento.ClienteId, evento.PropostaId, evento.NomeCliente, evento.QuantidadeCartoes, evento.LimitePorCartao);

            db.Cartoes.AddRange(cartoes);
            await db.SaveChangesAsync();

            _logger.LogInformation("{Quantidade} cartão(ões) emitido(s) para o cliente {ClienteId}", cartoes.Count, evento.ClienteId);
        }

        public override void Dispose()
        {
            _canal?.Dispose();
            base.Dispose();
        }
    }
}