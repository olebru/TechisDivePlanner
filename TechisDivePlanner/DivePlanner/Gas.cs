using System;
using System.Collections.Generic;
using System.Text;

namespace DivePlanner
{
    public class Gas : IGas
    {
        public double pO2 { get ; set; }
        public double pN2 { get; set; }
        public double pHe { get; set; }
    }
}
