using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hamko.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        public string Name { get; set; }

        public string? Image { get; set; }

        public bool Status { get; set; } = true;

        public int? ParentId { get; set; }

        public virtual Group Parent { get; set; }

        public virtual ICollection<Group> Children { get; set; } = new List<Group>();
    }
}
