using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Canonical;
using LPR381Proj.Models;
namespace LPR381Proj.Algorithms
{
    public class PrimalSimplexSolver
    {
        private const int MaxIterations = 1000;

        public Solution Solve(CanonicalForm cf)
        {
            Solution solution = new Solution();
            int rows = cf.NumConstraints + 1;
            int cols = cf.NumVariables + 1;
            double[,] matrix = cf.Tableau;

            int iteration = 0;
            while (true)
            {
                solution.TableauIterations.Add(RenderTableau(cf, matrix, iteration));

                // 1. Check Optimality (Most negative entry in Row 0)
                int pivotCol = -1;
                double minCoeff = -1e-9; // Tolerance

                for (int j = 0; j < cols - 1; j++)
                {
                    if (matrix[0, j] < minCoeff)
                    {
                        minCoeff = matrix[0, j];
                        pivotCol = j;
                    }
                }

                if (pivotCol == -1)
                {
                    solution.Status = SolutionStatus.Optimal;
                    break;
                }

                // 2. Minimum Ratio Test
                int pivotRow = -1;
                double minRatio = double.MaxValue;

                for (int i = 1; i < rows; i++)
                {
                    if (matrix[i, pivotCol] > 1e-9)
                    {
                        double ratio = matrix[i, cols - 1] / matrix[i, pivotCol];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    solution.Status = SolutionStatus.Unbounded;
                    return solution;
                }

                // Update basis variable track
                cf.BasicVariables[pivotRow - 1] = cf.VariableNames[pivotCol];

                // 3. Perform Pivot Operation
                double pivotVal = matrix[pivotRow, pivotCol];

                for (int j = 0; j < cols; j++)
                {
                    matrix[pivotRow, j] /= pivotVal;
                }

                for (int i = 0; i < rows; i++)
                {
                    if (i != pivotRow)
                    {
                        double factor = matrix[i, pivotCol];
                        for (int j = 0; j < cols; j++)
                        {
                            matrix[i, j] -= factor * matrix[pivotRow, j];
                        }
                    }
                }

                iteration++;
                if (iteration > MaxIterations)
                {
                    solution.Status = SolutionStatus.IterationLimitReached;
                    return solution;
                }
            }

            // Infeasibility check
            for (int i = 0; i < cf.NumConstraints; i++)
            {
                string basicName = cf.BasicVariables[i];
                int basicColIdx = cf.VariableNames.IndexOf(basicName);
                if (cf.ArtificialColumns.Contains(basicColIdx) && Math.Abs(matrix[i + 1, cols - 1]) > 1e-6)
                {
                    solution.Status = SolutionStatus.Infeasible;
                    return solution;
                }
            }

            // Extract raw values for every column (Rounded to 3 decimal places)
            var rawValues = new Dictionary<string, double>();
            for (int j = 0; j < cf.VariableNames.Count; j++)
            {
                string varName = cf.VariableNames[j];
                int basicIndex = cf.BasicVariables.IndexOf(varName);
                rawValues[varName] = basicIndex != -1 ? Math.Round(matrix[basicIndex + 1, cols - 1], 3) : 0.0;
            }

            // Reconstruct ORIGINAL decision variables (Rounded to 3 decimal places)
            foreach (var map in cf.VariableMap)
            {
                double posVal = rawValues[cf.VariableNames[map.PosIndex]];

                if (map.Kind == VariableType.NonPositive)
                {
                    solution.VariableValues[map.OriginalName] = Math.Round(-posVal, 3);
                }
                else
                {
                    double negVal = map.NegIndex != -1 ? rawValues[cf.VariableNames[map.NegIndex]] : 0.0;
                    solution.VariableValues[map.OriginalName] = Math.Round(posVal - negVal, 3);
                }
            }

            // Surface slack/surplus values
            for (int j = 0; j < cf.VariableNames.Count; j++)
            {
                if (cf.ArtificialColumns.Contains(j)) continue;
                bool isDecisionColumn = cf.VariableMap.Any(m => m.PosIndex == j || m.NegIndex == j);
                if (!isDecisionColumn)
                    solution.VariableValues[cf.VariableNames[j]] = rawValues[cf.VariableNames[j]];
            }

            // Final Z calculation (Rounded to 3 decimal places)
            double rawZ = matrix[0, cols - 1];
            double finalZ = cf.Objective == ObjectiveType.Minimize ? -rawZ : rawZ;
            solution.OptimalValue = Math.Round(finalZ, 3);

            return solution;
        }

        private string RenderTableau(CanonicalForm cf, double[,] matrix, int iteration)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"--- Tableau Iteration {iteration} ---");
            sb.Append(string.Format("{0,-8}", "Basic"));

            foreach (var vName in cf.VariableNames)
            {
                sb.Append(string.Format("{0,10}", vName));
            }
            sb.AppendLine(string.Format("{0,10}", "RHS"));

            // Row 0 (Z)
            sb.Append(string.Format("{0,-8}", "Z"));
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                sb.Append(string.Format("{0,10:0.00}", matrix[0, j]));
            }
            sb.AppendLine();

            // Constraint Rows
            for (int i = 1; i < matrix.GetLength(0); i++)
            {
                sb.Append(string.Format("{0,-8}", cf.BasicVariables[i - 1]));
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    sb.Append(string.Format("{0,10:0.00}", matrix[i, j]));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}