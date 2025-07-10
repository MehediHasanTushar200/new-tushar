using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hamko.Models
{
    public class StockIn
    {
        [Key]
        public int Id { get; set; }

        public int? PurchaseId { get; set; }
        public Purchase Purchase { get; set; }


        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }
}
