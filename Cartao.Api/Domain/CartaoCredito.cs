namespace Cartao.Api.Domain
{
    public class CartaoCredito
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public Guid PropostaId { get; set; }
        public string NomeTitular { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public decimal Limite { get; set; }
        public DateTime DataEmissao { get; set; }
    }
}
