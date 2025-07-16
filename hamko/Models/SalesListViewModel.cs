using hamko.Models;

namespace hamko.Models
{
    public class SalesListViewModel
    {
        public List<Sales> Sales { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

