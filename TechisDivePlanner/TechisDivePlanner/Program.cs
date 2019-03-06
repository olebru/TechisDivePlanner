using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechisDivePlanner
{
    class Program
    {
        static void Main(string[] args)
        {
            var b = new Buhlmann.TranslatedDecoCalc();
            b.Run();
            Console.ReadLine();

        }
    }
}
