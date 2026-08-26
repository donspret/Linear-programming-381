using System;
using System.IO;
using LPR381Proj.Algorithms;
using LPR381Proj.Input;
using LPR381Proj.Models;

namespace LPR381Proj.Menus.AlgoMenu
{
    internal class RevisedSimplexMenu
    {
        public void Run()
        {
            Console.Clear();

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "      REVISED PRIMAL SIMPLEX");

            Console.WriteLine(
                "========================================");

            Console.WriteLine();

            Console.Write(
                "Enter input file path: ");

            string inputPath =
                Console.ReadLine();

            if (inputPath == null)
                return;

            inputPath =
                inputPath.Trim().Trim('"');

            string errorMessage;

            if (!InputValidator.ValidateFile(
                inputPath,
                out errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Input error: " +
                    errorMessage);

                return;
            }

            try
            {
                InputParser parser =
                    new InputParser();

                LinearProgram lp =
                    parser.ParseFile(
                        inputPath);

                RevisedSimplexSolver solver =
                    new RevisedSimplexSolver();

                string result =
                    solver.Solve(lp);

                Console.WriteLine();
                Console.WriteLine(result);

                string directory =
                    Path.GetDirectoryName(
                        inputPath);

                if (string.IsNullOrEmpty(directory))
                {
                    directory =
                        Environment.CurrentDirectory;
                }

                string outputPath =
                    Path.Combine(
                        directory,
                        "RevisedSimplexOutput.txt");

                File.WriteAllText(
                    outputPath,
                    result);

                Console.WriteLine();
                Console.WriteLine(
                    "Output saved to:");

                Console.WriteLine(
                    outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Error: " +
                    ex.Message);
            }
        }
    }
}