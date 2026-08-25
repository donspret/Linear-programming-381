using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Algorithms;
using LPR381Proj.Models;
using LPR381Proj.Output;
using LPR381Proj.Input; // Added missing namespace

namespace LPR381Proj.Menus.AlgoMenu
{
    public class CuttingPlaneMenu
    {
        public void Run()
        {
            Console.Clear();
            Console.WriteLine("=== WELCOME TO CUTTING PLANE SOLVER ===");
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
            Console.Write($"Enter file path or select pre-loaded files (Press Enter for default - InputFile1.txt): ");
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

            CuttingPlaneSolver solver = new CuttingPlaneSolver();

            string targetDirectory = Path.GetDirectoryName(inputPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputFileName = $"OutputResult_CuttingPlane_{timestamp}.txt";
            string outputPath = Path.Combine(targetDirectory, outputFileName);

            using (MultiWriter writer = new MultiWriter(outputPath))
            {
                // Run solver and pass the output writer
                Solution solution = solver.Solve(lp, writer);
            }

            Console.WriteLine($"[SUCCESS] Cutting Plane Output saved to: {outputPath}\n");
        }
    }
}