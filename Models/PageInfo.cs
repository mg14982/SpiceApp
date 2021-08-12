using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models
{
    public class PageInfo
    {
        public int TotalItems { get; set; }
        public int ItemsPerpage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPage => (int)Math.Ceiling((decimal)TotalItems / ItemsPerpage);
        public string utlParam { get; set; }
    }
}
