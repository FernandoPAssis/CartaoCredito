using Cartao.Api.Data;
using Cartao.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartao.Api.Controllers
{
    [ApiController]
    [Route("api/v1/cartoes")]
    public class CartoesController : ControllerBase
    {
        private readonly CartaoDbContext _db;

        public CartoesController(CartaoDbContext db)
        {
            _db = db;
        }

        [HttpGet("{clienteId:guid}")]
        public async Task<IActionResult> ListarPorCliente(Guid clienteId)
        {
            // equivale a: SELECT * FROM Cartoes WHERE ClienteId = @clienteId
            List<CartaoCredito> cartoes = await _db.Cartoes.Where(c => c.ClienteId == clienteId).ToListAsync();

            return Ok(cartoes);     // 200 (lista vazia se o cliente não tiver cartões)
        }
    }
}