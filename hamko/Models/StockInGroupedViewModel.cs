namespace hamko.ViewModels
{
    public class StockInGroupedViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }

    public class StockInSearchViewModel
    {
        public List<StockInGroupedViewModel> Results { get; set; }
    }
}
