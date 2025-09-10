using Microsoft.EntityFrameworkCore;

namespace ShopMaster.Models.DTO
{
    public class CommandeDto
    {
        public string ClientId { get; set; } = string.Empty;  // FK vers ApplicationUser

        [Precision(16, 2)]
        public decimal FraisLivraison { get; set; }

        public string AdresseLivraison { get; set; } = string.Empty;
        public string MethodePaiement { get; set; } = "paypal";
        public string StatutPaiement { get; set; } = "Payé";
        public string StatutCommande { get; set; } = "Payé";
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
