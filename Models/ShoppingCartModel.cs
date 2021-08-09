using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models
{
    public class ShoppingCartModel
    {
        public ShoppingCartModel()
        {
            Count = 1;
        }
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        [NotMapped]
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }
        [NotMapped]
        [ForeignKey("MenuItemId")]
        public MenuItem MenuItem { get; set; }
        public int MenuItemId { get; set; }
        [Range(1,int.MaxValue, ErrorMessage = "Please enter a value greater then or equal to {1}")]
        public int Count { get; set; }
    }
}
