using System;
using System.ComponentModel.DataAnnotations;


namespace hamko.Models
{
    public class Purchase
    {
        [Key]
        public int Id { get; set; }

        public string? InvoiceNo { get; set; }
        public string? RefNo { get; set; }
        public DateTime Date { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public List<StockIn> StockIns { get; set; } = new List<StockIn>();
        public string? Status { get; set; } = "Active";
       

    }
}
