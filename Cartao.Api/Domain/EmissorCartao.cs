using Cartao.Api.Domain;

namespace Cartao.Api.Domain
{
    public static class EmissorCartao
    {
        public const int MaximoCartoes = 2;

        // Emite a quantidade de cartões aprovada na proposta (no máximo 2),
        // cada um com o limite definido pela faixa de score.
        public static List<CartaoCredito> Emitir(Guid clienteId, Guid propostaId, string nomeTitular, int quantidade, decimal limitePorCartao)
        {
            List<CartaoCredito> cartoes = new List<CartaoCredito>();

            if (quantidade > MaximoCartoes)
                quantidade = MaximoCartoes;

            for (int i = 0; i < quantidade; i++)
            {
                CartaoCredito cartao = new CartaoCredito
                {
                    Id = Guid.NewGuid(),
                    ClienteId = clienteId,
                    PropostaId = propostaId,
                    NomeTitular = nomeTitular,
                    Numero = GerarNumero(),
                    Limite = limitePorCartao,
                    DataEmissao = DateTime.UtcNow
                };

                cartoes.Add(cartao);
            }

            return cartoes;
        }

        // Número fictício de 16 dígitos (apenas para simulação)
        private static string GerarNumero()
        {
            string numero = "";
            for (int i = 0; i < 16; i++)
            {
                numero += Random.Shared.Next(0, 10).ToString();
            }
            return numero;
        }
    }
}

