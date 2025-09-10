using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopMaster.Models;

namespace ShopMaster.Context
{
    public class ApplicationDbContext:IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Produit> Produit { get; set; }
        public DbSet<Commande> Commande { get; set; }
        public DbSet<LigneCommande> LigneCommande { get;set; }
    }
}
