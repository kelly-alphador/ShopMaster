using System.ComponentModel.DataAnnotations;

namespace ShopMaster.Models
{
    public class CartItem
    {
        public int ProduitId { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantite { get; set; }

        public string ImageUrl { get; set; }

        // Propriété de navigation (optionnelle)
        public Produit Produit { get; set; }

        // Calcul du total pour cet item
        public decimal Total => PrixUnitaire * Quantite;
    }
}
