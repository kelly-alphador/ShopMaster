namespace ShopMaster.Models
{
    public class FactureItem
    {
        public string NomProduit { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal PrixTotal { get; set; }
    }
}
