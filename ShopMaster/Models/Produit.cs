using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ShopMaster.Models
{
    public class Produit
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(50),Required]
        public string Nom { get; set; } = string.Empty;
        [MaxLength(50), Required]
        public string Marque { get; set; } = string.Empty;
        [MaxLength(3), Required]
        public int qte {  get; set; }
        [Required, MaxLength(100)]
        public string Category { get; set; } = "";

        [Precision(16,2), Required]
        public decimal Prix { get; set; }
        [MaxLength(100), Required]
        public string Description { get; set; } = string.Empty;
        [MaxLength(150), Required]
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }=DateTime.UtcNow;
        // Navigation
        public ICollection<LigneCommande> LignesCommande { get; set; } = new List<LigneCommande>();
    }
}
