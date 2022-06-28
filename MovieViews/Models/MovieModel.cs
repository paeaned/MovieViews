using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieViews.Models
{
    public class MovieModel
    {
        public int Num { get; set; }
        public string Name { get; set; }
        public string ODate { get; set; }
        public long TEarn { get; set; }
        public double Pct { get; set; }
        public long TAud { get; set; }
    }
}
