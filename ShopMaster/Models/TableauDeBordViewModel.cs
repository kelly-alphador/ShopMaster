using ShopMaster.Controllers;

namespace ShopMaster.Models
{
    public class TableauDeBordViewModel
    {
        public int NombreTotalProduits { get; set; }
        public int NouveauxClientsInscrits { get; set; }
        public List<ClientCommandeViewModel> ClientsAvecPlusCommandes { get; set; } = new List<ClientCommandeViewModel>();
        public List<ProduitVenduViewModel> Top5ProduitsVendus { get; set; } = new List<ProduitVenduViewModel>();
        public List<ChiffreAffaireSemaineViewModel> ChiffreAffairesSemaine { get; set; } = new List<ChiffreAffaireSemaineViewModel>();
    }
}
