namespace Cartao.Api.Messaging
{
    // Mensagem recebida do serviço de Proposta (mesmo formato que ele publica)
    public class PropostaGeradaEvento
    {
        public Guid PropostaId { get; set; }
        public Guid ClienteId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool Aprovada { get; set; }
        public int QuantidadeCartoes { get; set; }
        public decimal LimitePorCartao { get; set; }
    }
}
