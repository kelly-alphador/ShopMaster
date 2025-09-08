using Microsoft.EntityFrameworkCore;

namespace ShopMaster.Models
{
    public class LigneCommande
    {
        public int Id { get; set; }
        public int CommandeId { get; set; }   // FK
        public int ProduitId { get; set; }    // FK
        public int Quantite { get; set; }
        [Precision(16,2)]
        public decimal PrixUnitaire { get; set; }

        // Navigation
        public Commande Commande { get; set; }
        public Produit Produit { get; set; }
    }
}
