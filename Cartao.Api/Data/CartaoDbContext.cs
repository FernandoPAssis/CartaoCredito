using Cartao.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cartao.Api.Data
{
    public class CartaoDbContext : DbContext
    {
        public CartaoDbContext(DbContextOptions<CartaoDbContext> options) : base(options)
        {
        }

        public DbSet<CartaoCredito> Cartoes
        {
            get { return Set<CartaoCredito>(); }
        }
    }
}