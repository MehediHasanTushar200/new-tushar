using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hamko.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        public string Name { get; set; }

        public string? Image { get; set; } 

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public string? Description { get; set; }

        public bool Status { get; set; }

        [Required(ErrorMessage = "Group selection is required")]
        public int GroupId { get; set; }

        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        [Required(ErrorMessage = "Item selection is required")]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public Item Item { get; set; }
    }
}
