namespace ShopMaster.Models
{
    public class FactureViewModel
    {
        public int CommandeId { get; set; }
        public List<FactureItem> Items { get; set; } = new List<FactureItem>();
        public decimal PrixTotalCommande { get; set; }
        public DateTime DateCommande { get; set; }
        public string EmailClient { get; set; }
        public string NomClient { get; set; }
        public string NumeroCommande => $"CMD{CommandeId:D6}";
    }
}
