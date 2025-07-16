namespace hamko.Models
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
    }
}
