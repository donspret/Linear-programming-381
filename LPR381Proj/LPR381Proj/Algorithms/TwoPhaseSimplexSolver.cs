using System;
using System.Collections.Generic;
using LPR381Proj.Canonical;
using LPR381Proj.Models;

namespace LPR381Proj.Solvers
{
    public class TwoPhaseSimplexSolver
    {
        private const double Epsilon = 1e-7;

        public void SolveAndPrint(CanonicalForm cf)
        {
            Console.WriteLine("==================== PHASE 1 ITERATIONS ====================");

            // 1. Identify artificial variables dynamically
            List<int> artificialCols = new List<int>();
            for (int j = 0; j < cf.NumVariables; j++)
            {
                if (cf.VariableNames[j].StartsWith("a", StringComparison.OrdinalIgnoreCase))
                {
                    artificialCols.Add(j);
                }
            }

            // 2. Set up W-Row (Row 0 for Phase 1)
            // W = sum of artificial variables. Initialized to zero out basic artificials.
            double[] wRow = new double[cf.NumVariables + 1];
            foreach (int aCol in artificialCols)
            {
                int basicRow = cf.BasicVariables.IndexOf(cf.VariableNames[aCol]);
                if (basicRow != -1)
                {
                    // Row substitution: W = W - ConstraintRow
                    for (int j = 0; j <= cf.NumVariables; j++)
                    {
                        wRow[j] -= cf.Tableau[basicRow + 1, j];
                    }
                }
            }

            int iteration = 0;
            PrintTwoPhaseTableau(cf, wRow, iteration);

            // --- Phase 1 Simplex Loop ---
            while (true)
            {
                int pivotCol = GetPivotColumnPhase1(wRow, cf.NumVariables);
                if (pivotCol == -1) break; // Phase 1 Optimal reached

                int pivotRow = GetPivotRow(cf, pivotCol);
                if (pivotRow == -1)
                {
                    Console.WriteLine("Status: Unbounded in Phase 1");
                    return;
                }

                Pivot(cf, wRow, pivotRow, pivotCol);
                iteration++;
                PrintTwoPhaseTableau(cf, wRow, iteration);
            }

            double sumArtificials = Math.Abs(wRow[cf.NumVariables]);

            // --- Check Infeasibility ---
            if (sumArtificials > Epsilon)
            {
                Console.WriteLine("==================== FINAL RESULT ====================");
                Console.WriteLine("Status: Infeasible");
                Console.WriteLine($"Artificial sum W = {sumArtificials:F2} (Non-zero)");
                Console.WriteLine("==================================================");
                return;
            }

            Console.WriteLine("Phase 1 Complete: Feasible region found. Transitioning to Phase 2...\n");
            // Proceed to Phase 2 (standard Z-row pivoting) if feasible
        }

        private int GetPivotColumnPhase1(double[] wRow, int numVars)
        {
            int minCol = -1;
            double minVal = -Epsilon;

            for (int j = 0; j < numVars; j++)
            {
                if (wRow[j] < minVal)
                {
                    minVal = wRow[j];
                    minCol = j;
                }
            }
            return minCol;
        }

        private int GetPivotRow(CanonicalForm cf, int pivotCol)
        {
            int minRow = -1;
            double minRatio = double.MaxValue;

            for (int i = 1; i <= cf.NumConstraints; i++)
            {
                double val = cf.Tableau[i, pivotCol];
                if (val > Epsilon)
                {
                    double ratio = cf.Tableau[i, cf.NumVariables] / val;
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        minRow = i;
                    }
                }
            }
            return minRow;
        }

        private void Pivot(CanonicalForm cf, double[] wRow, int pivotRow, int pivotCol)
        {
            double pivotVal = cf.Tableau[pivotRow, pivotCol];

            // Normalize Pivot Row
            for (int j = 0; j <= cf.NumVariables; j++)
            {
                cf.Tableau[pivotRow, j] /= pivotVal;
            }

            // Update Basic Variable Label
            cf.BasicVariables[pivotRow - 1] = cf.VariableNames[pivotCol];

            // Eliminate W Row
            double wFactor = wRow[pivotCol];
            for (int j = 0; j <= cf.NumVariables; j++)
            {
                wRow[j] -= wFactor * cf.Tableau[pivotRow, j];
                if (Math.Abs(wRow[j]) < Epsilon) wRow[j] = 0.0;
            }

            // Eliminate Tableau Constraint Rows and Row 0 (Z)
            for (int i = 0; i <= cf.NumConstraints; i++)
            {
                if (i != pivotRow)
                {
                    double factor = cf.Tableau[i, pivotCol];
                    for (int j = 0; j <= cf.NumVariables; j++)
                    {
                        cf.Tableau[i, j] -= factor * cf.Tableau[pivotRow, j];
                        if (Math.Abs(cf.Tableau[i, j]) < Epsilon) cf.Tableau[i, j] = 0.0;
                    }
                }
            }
        }

        private void PrintTwoPhaseTableau(CanonicalForm cf, double[] wRow, int iter)
        {
            Console.WriteLine($"--- Tableau Iteration {iter} ---");
            Console.Write($"{"Basic",-8}");
            foreach (var v in cf.VariableNames)
            {
                Console.Write($"{v,10}");
            }
            Console.WriteLine($"{"RHS",10}");

            // Row W (Phase 1 Objective)
            Console.Write($"{"W",-8}");
            for (int j = 0; j < cf.NumVariables; j++)
            {
                Console.Write($"{FormatVal(wRow[j]),10}");
            }
            Console.WriteLine($"{FormatVal(wRow[cf.NumVariables]),10}");

            // Row Z (Original Objective)
            Console.Write($"{"Z",-8}");
            for (int j = 0; j < cf.NumVariables; j++)
            {
                Console.Write($"{FormatVal(cf.Tableau[0, j]),10}");
            }
            Console.WriteLine($"{FormatVal(cf.Tableau[0, cf.NumVariables]),10}");

            // Constraint Rows
            for (int i = 0; i < cf.NumConstraints; i++)
            {
                Console.Write($"{cf.BasicVariables[i],-8}");
                for (int j = 0; j < cf.NumVariables; j++)
                {
                    Console.Write($"{FormatVal(cf.Tableau[i + 1, j]),10}");
                }
                Console.WriteLine($"{FormatVal(cf.Tableau[i + 1, cf.NumVariables]),10}");
            }
            Console.WriteLine();
        }

        private string FormatVal(double val)
        {
            if (Math.Abs(val) < Epsilon) return "0,00";
            return val.ToString("F2");
        }
    }
}