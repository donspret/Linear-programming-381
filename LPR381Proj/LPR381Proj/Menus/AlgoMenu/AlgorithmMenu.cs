using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Models;
using LPR381Proj.Menus.AlgoMenu;

namespace LPR381Proj.Menus
{
    public class AlgorithmMenu
    {
        public void Run()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("       SELECT SOLVER ALGORITHM          ");
            Console.WriteLine("========================================");
            Console.WriteLine("1. Primal Simplex Algorithm");
            Console.WriteLine("2. Revised Primal Simplex Algorithm");
            Console.WriteLine("3. Branch & Bound Simplex Algorithm");
            Console.WriteLine("4. Branch & Bound Knapsack Algorithm");
            Console.WriteLine("5. Cutting Plane Algorithm");
            Console.WriteLine("========================================");
            Console.Write("Enter option (1-5): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    new PrimalSimplexMenu().Run();
                    break;
                case "5":
                    new CuttingPlaneMenu().Run();
                    break;
                default:
                    Console.WriteLine("Option not implemented or invalid.");
                    break;
            }
        }
    }
}