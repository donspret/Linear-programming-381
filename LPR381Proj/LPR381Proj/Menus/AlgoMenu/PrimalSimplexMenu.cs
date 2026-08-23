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
            Console.WriteLine("=== WELCOME TO PRIMAL SIMPLEX SOLVER ===");
            Console.WriteLine();
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

            Console.WriteLine("Pre-Loaded Files Avaiable:");
            Console.WriteLine("InputFile1.txt...Criteria Example");
            Console.WriteLine("InputFile2.txt...Max Primal Simplex Example");
            Console.WriteLine("InputFile3.txt...URS Example");
            Console.WriteLine("InputFile4.txt...Another Max Primal Simplex Example");
            Console.WriteLine("InputFile5.txt...Min Primal Simplex Example");
            Console.WriteLine("InputFile6.txt...Infeasible Primal Simplex Example");
            Console.WriteLine("InputFile7.txt...Unbounded Primal Simplex Example");
            Console.WriteLine();
            Console.WriteLine("You are otherwise welcome to upload your own new file located on your local PC.");
            Console.WriteLine();
            Console.Write($"Enter file path to upload your file or select pre-loaded files (Alternatively, press Enter to use default file - InputFile1.txt), see example file path : '{defaultPath}'): ");
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