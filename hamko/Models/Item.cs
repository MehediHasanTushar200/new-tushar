using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hamko.Models
{
  
            public class Item
            {
                public int Id { get; set; }

                [Required(ErrorMessage = "Item name is required")]
                public string Name { get; set; }

                public string Description { get; set; }

                public string Image { get; set; }  // store image path

                [Required]
                public int GroupId { get; set; }
                public virtual Group Group { get; set; }

                public bool Status { get; set; } = true;

                [NotMapped]
                public IFormFile ImageFile { get; set; }
                public DateTime CreatedDate { get; set; } = DateTime.Now;



    }
     
 
}
