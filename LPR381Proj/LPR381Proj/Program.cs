using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Input;
using LPR381Proj.Menus;

namespace LPR381Proj
{
    class Program
    {
        static void Main(string[] args)
        {
            AlgorithmMenu menu = new AlgorithmMenu();
            menu.Run();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}