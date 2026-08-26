using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LPR381Proj.Algorithms;
using LPR381Proj.Input;
using LPR381Proj.Models;
using LPR381Proj.Output;

namespace LPR381Proj.Menus.AlgoMenu
{
    public class BranchAndBoundMenu
    {
        public void Run()
        {
            Console.Clear();
            Console.WriteLine("=== WELCOME TO BRANCH & BOUND SIMPLEX SOLVER ===");
            Console.WriteLine();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string defaultPath = Path.Combine(baseDir, "InputFile.txt");

            if (!File.Exists(defaultPath))
            {
                string projectDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\"));
                string projectPath = Path.Combine(projectDir, "InputFile.txt");
                if (File.Exists(projectPath))
                {
                    defaultPath = projectPath;
                }
            }

            Console.WriteLine("Pre-Loaded Files Available:");
            Console.WriteLine("InputFile.txt....Binary Knapsack Example");
            Console.WriteLine("InputFile8.txt...Integer Programming Example");
            Console.WriteLine("InputFile9.txt...Binary Programming Example");
            Console.WriteLine();
            Console.WriteLine("You are otherwise welcome to upload your own new file located on your local PC.");
            Console.WriteLine();
            Console.Write($"Enter file path or select pre-loaded files (Press Enter for default: '{defaultPath}'): ");
            string inputPath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                inputPath = defaultPath;
            }

            if (!InputValidator.ValidateFile(inputPath, out string error))
            {
                Console.WriteLine($"\n[ERROR]: {error}");
                return;
            }

            InputParser parser = new InputParser();
            LinearProgram lp = parser.ParseFile(inputPath);

            if (!lp.VariableTypes.Any(t => t == VariableType.Integer || t == VariableType.Binary))
            {
                Console.WriteLine("\n[ERROR]: Branch & Bound requires at least one 'int' or 'bin' variable.");
                return;
            }

            IntegerProgram ip = new IntegerProgram(lp);

            BranchAndBoundSimplexSolver solver = new BranchAndBoundSimplexSolver();
            BranchAndBoundResult result = solver.Solve(ip);

            string targetDirectory = Path.GetDirectoryName(inputPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputFileName = $"OutputResult_BranchAndBound_{timestamp}.txt";
            string outputPath = Path.Combine(targetDirectory, outputFileName);

            TextWriter originalConsole = Console.Out;
            using (MultiWriter writer = new MultiWriter(outputPath))
            {
                Console.SetOut(writer);
                DisplayResult(result);
                Console.SetOut(originalConsole);
            }

            Console.WriteLine($"[SUCCESS] Output saved to: {outputPath}\n");
        }

        private void DisplayResult(BranchAndBoundResult result)
        {
            foreach (BranchAndBoundNodeResult node in result.Nodes)
            {
                Console.WriteLine();
                Console.WriteLine("========================================================");
                Console.WriteLine($"Node {node.NodeId} (Parent: {(node.ParentId == 0 ? "-" : node.ParentId.ToString())})");
                Console.WriteLine($"Branch: {node.BranchDescription}");
                Console.WriteLine("========================================================");

                Console.WriteLine("--- Canonical Form ---");
                Console.WriteLine("Variables: " + string.Join(", ", node.CanonicalForm.VariableNames));
                Console.WriteLine("Initial Basic Variables: " + string.Join(", ", node.CanonicalForm.BasicVariables));

                Console.WriteLine("\n--- Tableau Iterations ---");
                foreach (string iteration in node.Solution.TableauIterations)
                {
                    Console.WriteLine(iteration);
                }

                Console.WriteLine($"Status: {node.Solution.Status}");
                if (node.Solution.Status == SolutionStatus.Optimal)
                {
                    Console.WriteLine($"Relaxed Objective Value (Z): {node.Solution.OptimalValue:0.000}");
                    foreach (KeyValuePair<string, double> kvp in node.Solution.VariableValues.Where(k => k.Key.StartsWith("x")))
                    {
                        Console.WriteLine($"  {kvp.Key} = {kvp.Value:0.000}");
                    }
                }

                if (node.Fathomed)
                {
                    Console.WriteLine(node.FathomReason);
                }
            }

            Console.WriteLine();
            Console.WriteLine("========================================================");
            Console.WriteLine("BEST CANDIDATE");
            Console.WriteLine("========================================================");

            if (result.BestSolution == null)
            {
                Console.WriteLine("No integer-feasible solution was found.");
            }
            else
            {
                Console.WriteLine($"Node: {result.BestNodeId}");
                Console.WriteLine($"Optimal Objective Value (Z): {result.BestSolution.OptimalValue:0.000}");
                Console.WriteLine("Decision Variable Values:");
                foreach (KeyValuePair<string, double> kvp in result.BestSolution.VariableValues.Where(k => k.Key.StartsWith("x")))
                {
                    Console.WriteLine($"  {kvp.Key} = {kvp.Value:0.000}");
                }
            }

            if (result.NodeLimitReached)
            {
                Console.WriteLine("\n[WARNING]: Node limit reached before the tree was fully explored.");
            }
        }
    }
}
