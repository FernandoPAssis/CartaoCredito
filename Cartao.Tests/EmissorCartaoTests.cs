using Cartao.Api.Domain;

namespace Cartao.Tests
{
    public class EmissorCartaoTests
    {
        [Fact]
        public void Emitir_QuandoQuantidadeZero_NaoEmiteCartoes()
        {
            List<CartaoCredito> cartoes = EmissorCartao.Emitir(Guid.NewGuid(), Guid.NewGuid(), "Cliente Teste", 0, 0);

            Assert.Empty(cartoes);
        }

        [Fact]
        public void Emitir_QuandoUmCartaoAprovado_EmiteUmCartaoDeMilReais()
        {
            // Arrange
            Guid clienteId = Guid.NewGuid();

            // Act
            List<CartaoCredito> cartoes = EmissorCartao.Emitir(clienteId, Guid.NewGuid(), "Cliente Teste", 1, 1000);

            // Assert
            Assert.Single(cartoes);
            Assert.Equal(1000m, cartoes[0].Limite);
            Assert.Equal(clienteId, cartoes[0].ClienteId);
        }

        [Fact]
        public void Emitir_QuandoDoisCartoesAprovados_EmiteDoisCartoesDeCincoMilReaisCada()
        {
            List<CartaoCredito> cartoes = EmissorCartao.Emitir(Guid.NewGuid(), Guid.NewGuid(), "Cliente Teste", 2, 5000);

            Assert.Equal(2, cartoes.Count);
            Assert.Equal(5000m, cartoes[0].Limite);
            Assert.Equal(5000m, cartoes[1].Limite);
        }

        [Fact]
        public void Emitir_QuandoQuantidadeMaiorQueMaximo_EmiteNoMaximoDoisCartoes()
        {
            List<CartaoCredito> cartoes = EmissorCartao.Emitir(Guid.NewGuid(), Guid.NewGuid(), "Cliente Teste", 5, 5000);

            Assert.Equal(EmissorCartao.MaximoCartoes, cartoes.Count);
        }

        [Fact]
        public void Emitir_SempreGeraNumeroComDezesseisDigitos()
        {
            List<CartaoCredito> cartoes = EmissorCartao.Emitir(Guid.NewGuid(), Guid.NewGuid(), "Cliente Teste", 2, 5000);

            foreach (CartaoCredito cartao in cartoes)
            {
                Assert.Equal(16, cartao.Numero.Length);
            }
        }
    }
}