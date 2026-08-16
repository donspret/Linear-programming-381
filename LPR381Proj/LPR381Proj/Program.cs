using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Input;
using LPR381Proj.Menus.AlgoMenu;

namespace LPR381Proj
{
    class Program
    {
        static void Main(string[] args)
        {
            PrimalSimplexMenu menu = new PrimalSimplexMenu();
            menu.Run();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}