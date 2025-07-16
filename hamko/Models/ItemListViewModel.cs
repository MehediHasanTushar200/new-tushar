namespace hamko.Models
{
    public class ItemListViewModel
    {
        public List<Item> Items { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
    }
}
