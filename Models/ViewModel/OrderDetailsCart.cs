using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models.ViewModel
{
    public class OrderDetailsCart
    {
        public List<ShoppingCartModel> ListCart { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
