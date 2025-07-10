using System.ComponentModel.DataAnnotations;

namespace hamko.Models
{
    public class StockOut
    {
        [Key]
        public int Id { get; set; }

        public int? SalesId { get; set; }
        public Sales Sales { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
