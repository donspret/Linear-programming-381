using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Canonical;
using LPR381Proj.Models;

namespace LPR381Proj.Output
{
    public static class OutputResults
    {
        public static void DisplayCanonicalForm(CanonicalForm cf)
        {
            Console.WriteLine("\n================ CANONICAL FORM ================");
            Console.WriteLine("Variables: " + string.Join(", ", cf.VariableNames));
            Console.WriteLine("Initial Basic Variables: " + string.Join(", ", cf.BasicVariables));
            Console.WriteLine("================================================\n");
        }

        public static void DisplaySolution(Solution solution)
        {
            Console.WriteLine("\n================ SIMPLEX ITERATIONS ================");
            foreach (var iteration in solution.TableauIterations)
            {
                Console.WriteLine(iteration);
            }

            Console.WriteLine("================ FINAL RESULT ================");
            Console.WriteLine($"Status: {solution.Status}");
            if (solution.Status == SolutionStatus.Optimal)
            {
                Console.WriteLine($"Optimal Objective Value (Z): {solution.OptimalValue:0.000}");
                Console.WriteLine("\nDecision Variable Values:");
                foreach (var kvp in solution.VariableValues)
                {
                    Console.WriteLine($"  {kvp.Key} = {kvp.Value:0.000}");
                }
            }
            Console.WriteLine("==============================================\n");
        }
    }
}
