using System;
using System.IO;
using LPR381Proj.Canonical;
using LPR381Proj.Input;
using LPR381Proj.Models;
using LPR381Proj.Solvers;

namespace LPR381Proj.Menus.AlgoMenu
{
    public class TwoPhaseSimplexMenu
    {
        public void Run()
        {
            Console.Clear();
            Console.WriteLine("=== WELCOME TO TWO-PHASE SIMPLEX SOLVER ===");
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

            Console.WriteLine("You are welcome to upload your own new file located on your local PC.");
            Console.WriteLine();
            Console.Write("Enter file path or select pre-loaded files (Press Enter for default): ");
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

            CanonicalForm cf = new CanonicalForm();

            TwoPhaseSimplexSolver solver = new TwoPhaseSimplexSolver();

            Console.WriteLine();
            solver.SolveAndPrint(cf);

            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
        }
    }
}