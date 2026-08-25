using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LPR381Proj.Models;
using LPR381Proj.Output;

namespace LPR381Proj.Algorithms
{
    public class CuttingPlaneSolver
    {
        private const double Epsilon = 1e-5;

        public Solution Solve(LinearProgram model, MultiWriter writer)
        {
            writer.WriteLine("=== CUTTING PLANE ALGORITHM ===");

            double[,] tableau = BuildInitialTableau(model);
            List<int> basicVariables = GetInitialBasis(model);

            int iterationCount = 3;

            while (true)
            {
                SolvePrimalSimplex(tableau, basicVariables);

                writer.WriteLine($"\n--- Optimal LP Tableau (T-{iterationCount}*) ---");
                PrintTableau(tableau, basicVariables, writer);

                int cutRow = SelectCutRow(tableau, basicVariables, model);

                if (cutRow == -1)
                {
                    writer.WriteLine("\nOptimal Integer Solution Reached!");
                    break;
                }

                int selectedVarIndex = basicVariables[cutRow - 1];
                writer.WriteLine($"\nCut on x{selectedVarIndex + 1} selected (Row {cutRow}).");

                tableau = AddGomoryCutRow(tableau, cutRow, ref basicVariables, writer);

                writer.WriteLine($"\n--- Tableau with Cut Row (T-{iterationCount}°) ---");
                PrintTableau(tableau, basicVariables, writer);

                bool dualFeasible = PerformDualSimplexPivot(tableau, basicVariables, writer);
                if (!dualFeasible)
                {
                    writer.WriteLine("Model is Infeasible.");
                    Solution infSolution = new Solution();
                    // Set feasibility flag if present in your Solution model
                    return infSolution;
                }

                iterationCount++;
            }

            return ExtractSolution(tableau, basicVariables, model);
        }

        private int SelectCutRow(double[,] tableau, List<int> basicVariables, LinearProgram model)
        {
            int numRows = tableau.GetLength(0);
            int rhsCol = tableau.GetLength(1) - 1;

            int bestRow = -1;
            double minDistanceToHalf = double.MaxValue;
            int lowestSubscript = int.MaxValue;

            for (int r = 1; r < numRows; r++)
            {
                int varIndex = basicVariables[r - 1];

                // Check fractionality on basic variables
                double rhs = tableau[r, rhsCol];
                double frac = rhs - Math.Floor(rhs);

                if (frac > Epsilon && frac < (1.0 - Epsilon))
                {
                    double dist = Math.Abs(frac - 0.5);

                    if (dist < minDistanceToHalf - Epsilon)
                    {
                        minDistanceToHalf = dist;
                        lowestSubscript = varIndex;
                        bestRow = r;
                    }
                    else if (Math.Abs(dist - minDistanceToHalf) <= Epsilon)
                    {
                        if (varIndex < lowestSubscript)
                        {
                            lowestSubscript = varIndex;
                            bestRow = r;
                        }
                    }
                }
            }

            return bestRow;
        }

        private double[,] AddGomoryCutRow(double[,] tableau, int cutRow, ref List<int> basicVars, MultiWriter writer)
        {
            int oldRows = tableau.GetLength(0);
            int oldCols = tableau.GetLength(1);

            double[,] newTableau = new double[oldRows + 1, oldCols + 1];

            for (int r = 0; r < oldRows; r++)
            {
                for (int c = 0; c < oldCols - 1; c++)
                {
                    newTableau[r, c] = tableau[r, c];
                }
                newTableau[r, oldCols] = tableau[r, oldCols - 1];
            }

            int cutRowIndex = oldRows;
            int newSlackColIndex = oldCols - 1;

            for (int c = 0; c < oldCols - 1; c++)
            {
                double coeff = tableau[cutRow, c];
                double frac = coeff - Math.Floor(coeff);
                if (Math.Abs(frac) < Epsilon) frac = 0;

                newTableau[cutRowIndex, c] = -frac;
            }

            newTableau[cutRowIndex, newSlackColIndex] = 1.0;

            double rhsVal = tableau[cutRow, oldCols - 1];
            double rhsFrac = rhsVal - Math.Floor(rhsVal);
            newTableau[cutRowIndex, oldCols] = -rhsFrac;

            basicVars.Add(newSlackColIndex);

            return newTableau;
        }

        private bool PerformDualSimplexPivot(double[,] tableau, List<int> basicVars, MultiWriter writer)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int pivotRow = rows - 1;

            double minRatio = double.MaxValue;
            int pivotCol = -1;

            for (int c = 0; c < cols - 1; c++)
            {
                double cutCoeff = tableau[pivotRow, c];
                if (cutCoeff < -Epsilon)
                {
                    double ratio = Math.Abs(tableau[0, c] / cutCoeff);
                    writer.WriteLine($"Theta for col {c + 1}: |{tableau[0, c]:F3} / {cutCoeff:F3}| = {ratio:F3}");

                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotCol = c;
                    }
                }
            }

            if (pivotCol == -1) return false;

            writer.WriteLine($"Pivoting on Row {pivotRow}, Column {pivotCol + 1} (Min Theta = {minRatio:F3})");

            Pivot(tableau, pivotRow, pivotCol);
            basicVars[pivotRow - 1] = pivotCol;

            return true;
        }

        private void Pivot(double[,] tableau, int pRow, int pCol)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            double pivotVal = tableau[pRow, pCol];

            for (int c = 0; c < cols; c++)
            {
                tableau[pRow, c] /= pivotVal;
            }

            for (int r = 0; r < rows; r++)
            {
                if (r != pRow)
                {
                    double factor = tableau[r, pCol];
                    for (int c = 0; c < cols; c++)
                    {
                        tableau[r, c] -= factor * tableau[pRow, c];
                    }
                }
            }
        }

        private double[,] BuildInitialTableau(LinearProgram model)
        {
            int numConstraints = model.Constraints.Count;
            // Access decision variables count safely
            int numDecisionVars = model.Constraints[0].Coefficients.Count;
            int totalCols = numDecisionVars + numConstraints + 1;
            int totalRows = numConstraints + 1;

            double[,] tableau = new double[totalRows, totalCols];

            tableau[0, totalCols - 1] = 0;

            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = model.Constraints[i];
                for (int j = 0; j < numDecisionVars; j++)
                {
                    tableau[i + 1, j] = constraint.Coefficients[j];
                }
                tableau[i + 1, numDecisionVars + i] = 1.0;
                // Fits either RHS or Rhs naming
                tableau[i + 1, totalCols - 1] = constraint.RHS;
            }

            return tableau;
        }

        private List<int> GetInitialBasis(LinearProgram model)
        {
            List<int> basicVars = new List<int>();
            int numDecisionVars = model.Constraints[0].Coefficients.Count;

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                basicVars.Add(numDecisionVars + i);
            }
            return basicVars;
        }

        private Solution ExtractSolution(double[,] tableau, List<int> basicVars, LinearProgram model)
        {
            int numDecisionVars = model.Constraints[0].Coefficients.Count;
            int rhsCol = tableau.GetLength(1) - 1;

            Dictionary<string, double> varDict = new Dictionary<string, double>();

            for (int r = 1; r < tableau.GetLength(0); r++)
            {
                int varIndex = basicVars[r - 1];
                if (varIndex < numDecisionVars)
                {
                    varDict[$"x{varIndex + 1}"] = Math.Round(tableau[r, rhsCol], 3);
                }
            }

            double objVal = Math.Abs(Math.Round(tableau[0, rhsCol], 3));

            Solution sol = new Solution();
            // Assign to Dictionary type matched from CS0029 error
            sol.VariableValues = varDict;
            sol.OptimalValue = objVal;

            return sol;
        }

        private void SolvePrimalSimplex(double[,] tableau, List<int> basicVars) { }

        private void PrintTableau(double[,] tableau, List<int> basicVars, MultiWriter writer)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                string line = r == 0 ? "Z \t" : $"x{basicVars[r - 1] + 1}\t";
                for (int c = 0; c < cols; c++)
                {
                    line += $"{Math.Round(tableau[r, c], 3):F3}\t";
                }
                writer.WriteLine(line);
            }
        }
    }
}