using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LPR381Proj.Algorithms;
using LPR381Proj.Canonical;
using LPR381Proj.Input;
using LPR381Proj.Models;
using LPR381Proj.Output;
namespace LPR381Proj.Menus.AlgoMenu
{
    public class PrimalSimplexMenu
    {
        public void Run()
        {
            Console.Clear();
            Console.WriteLine("=== PRIMAL SIMPLEX SOLVER ===");

            // Check current execution directory first
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string defaultPath = Path.Combine(baseDir, "InputFile.txt");

            // If not found in bin/Debug, look up in the project folder
            if (!File.Exists(defaultPath))
            {
                string projectDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\"));
                string projectPath = Path.Combine(projectDir, "InputFile.txt");
                if (File.Exists(projectPath))
                {
                    defaultPath = projectPath;
                }
            }

            Console.Write($"Enter file path (Press Enter for default: '{defaultPath}'): ");
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

            CanonicalForm cf = CanonicalForm.ConvertToCanonical(lp);
            OutputResults.DisplayCanonicalForm(cf);

            PrimalSimplexSolver solver = new PrimalSimplexSolver();
            Solution solution = solver.Solve(cf);

            OutputResults.DisplaySolution(solution);
        }
    }
}