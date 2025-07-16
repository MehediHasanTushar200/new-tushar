namespace hamko.Models
{
    public class PurchaseListViewModel
    {
        public List<Purchase> Purchases { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

}
