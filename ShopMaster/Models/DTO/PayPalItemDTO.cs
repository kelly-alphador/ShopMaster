namespace ShopMaster.Models.DTO
{
    public class PayPalItemDTO
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Sku { get; set; }
        public string Currency { get; set; }
    }
}
