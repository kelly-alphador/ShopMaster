using Microsoft.EntityFrameworkCore;
using ShopMaster.Models;

namespace ShopMaster.Context
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Produit> Produit { get; set; }
    }
}
