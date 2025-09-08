using Microsoft.AspNetCore.Identity;

namespace ShopMaster.Models
{
    public class ApplicationUser:IdentityUser
    {
     
        public string Adress { get; set; } = "";
        public DateTime DateCreation { get; set; }

        // 🔑 Navigation : un client peut avoir plusieurs commandes
        public ICollection<Commande> Commandes { get; set; } = new List<Commande>();
    }
}
