using System.ComponentModel.DataAnnotations;

namespace hamko.Models
{
    public class Sales
    {
        [Key]
        public int Id { get; set; }

        public string? InvoiceNo { get; set; }
        public string? RefNo { get; set; }
        public DateTime Date { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public List<StockOut> StockOuts { get; set; }

        public string? Status { get; set; } = "Active";
    }
}
