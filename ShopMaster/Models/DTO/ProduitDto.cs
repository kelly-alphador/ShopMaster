using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ShopMaster.Models.DTO
{
    public class ProduitDto
    {
        [MaxLength(50), Required]
        public string Nom { get; set; } = string.Empty;
        [MaxLength(50), Required]
        public string Marque { get; set; } = string.Empty;
        [Required(ErrorMessage = "Le champ Quantité est obligatoire")]
        [Range(1,999,ErrorMessage ="Veuillez entrer un chiffre")]
        public int qte { get; set; }

        [Precision(16, 2)]
        [Range(0.01, 999999.99, ErrorMessage = "Veuillez entrer un chiffre")]
        [Required(ErrorMessage = "Le champ Quantité est obligatoire")]
        public decimal Prix { get; set; }
        [MaxLength(100)]
        [Required(ErrorMessage = "Le champ Quantité est obligatoire")]
        public string Description { get; set; } = string.Empty;
      
        public IFormFile? ImageUrl { get; set; }
    }
}
