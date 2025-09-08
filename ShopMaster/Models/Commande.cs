using Microsoft.EntityFrameworkCore;

namespace ShopMaster.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;   // FK vers ApplicationUser
        [Precision(16, 2)]
        public decimal FraisLivraison { get; set; }
        public string AdresseLivraison { get; set; } = string.Empty;
        public string MethodePaiement { get; set; } = string.Empty;
        public string StatutPaiement { get; set; } = "En attente";
        public string StatutCommande { get; set; } = "En attente";
        public DateTime DateCreation { get; set; } = DateTime.Now;

        //  Navigation
        public ApplicationUser Client { get; set; }
        //navigation
        public ICollection<LigneCommande> LignesCommande { get; set; } = new List<LigneCommande>();
    }
}
