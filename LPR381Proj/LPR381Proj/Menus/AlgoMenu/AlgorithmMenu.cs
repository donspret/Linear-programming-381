using System;
using LPR381Proj.Menus.AlgoMenu;

namespace LPR381Proj.Menus
{
    public class AlgorithmMenu
    {
        public void Run()
        {
            Console.Clear();

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "       SELECT SOLVER ALGORITHM");

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "1. Primal Simplex Algorithm");

            Console.WriteLine(
                "2. Revised Primal Simplex Algorithm");

            Console.WriteLine(
                "3. Branch & Bound Simplex Algorithm");

            Console.WriteLine(
                "4. Branch & Bound Knapsack Algorithm");

            Console.WriteLine(
                "5. Cutting Plane Algorithm");

            Console.WriteLine(
                "6. Two-Phase Simplex Algorithm");

            Console.WriteLine("7. Exit");

            Console.WriteLine(
                "========================================");

            Console.Write(
                "Enter option (1-7): ");

            string choice =
                Console.ReadLine();

            switch (choice)
            {
                case "1":
                    new PrimalSimplexMenu().Run();
                    break;

                case "2":
                    new RevisedSimplexMenu().Run();
                    break;

                case "3":
                    new BranchAndBoundMenu().Run();
                    break;

                case "4":
                    new KnapsackMenu().Run();
                    break;

                case "5":
                    new CuttingPlaneMenu().Run();
                    break;

                case "6":
                    new TwoPhaseSimplexMenu().Run();
                    break;

                case "7":
                    Console.WriteLine(
                        "Exiting the program.");
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine(
                        "Invalid option.");
                    break;
            }
        }
    }
}