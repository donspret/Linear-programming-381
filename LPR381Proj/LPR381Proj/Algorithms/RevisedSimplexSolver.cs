using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381Proj.Algorithms
{
    internal class RevisedSimplexSolver
    {
        private const double EPS = 0.0000001;

        public double[] LastSolution { get; private set; }
        public double LastObjectiveValue { get; private set; }

        public string Solve(
            double[] objective,
            double[,] constraints,
            double[] rhs,
            bool isMaximization = true)
        {
            ValidateInput(
                objective,
                constraints,
                rhs);

            int numberOfVariables =
                objective.Length;

            int numberOfConstraints =
                rhs.Length;

            // Revised Primal requires an initially feasible
            // basic solution for this implementation.
            for (int i = 0; i < rhs.Length; i++)
            {
                if (rhs[i] < -EPS)
                {
                    throw new InvalidOperationException(
                        "Revised Primal Simplex requires " +
                        "non-negative RHS values.");
                }
            }

            // Convert a minimization problem to max(-z).
            double[] workingObjective =
                new double[numberOfVariables];

            for (int i = 0; i < numberOfVariables; i++)
            {
                workingObjective[i] =
                    isMaximization
                        ? objective[i]
                        : -objective[i];
            }

            int totalVariables =
                numberOfVariables +
                numberOfConstraints;

            // A = [original columns | slack identity]
            double[,] fullA =
                new double[
                    numberOfConstraints,
                    totalVariables];

            for (int row = 0;
                 row < numberOfConstraints;
                 row++)
            {
                for (int col = 0;
                     col < numberOfVariables;
                     col++)
                {
                    fullA[row, col] =
                        constraints[row, col];
                }

                fullA[
                    row,
                    numberOfVariables + row] = 1;
            }

            // Costs of original variables and slacks.
            double[] costs =
                new double[totalVariables];

            for (int i = 0;
                 i < numberOfVariables;
                 i++)
            {
                costs[i] =
                    workingObjective[i];
            }

            // Initial basis = slack variables.
            int[] basis =
                new int[numberOfConstraints];

            for (int i = 0;
                 i < numberOfConstraints;
                 i++)
            {
                basis[i] =
                    numberOfVariables + i;
            }

            StringBuilder output =
                new StringBuilder();

            output.AppendLine(
                "============================================");

            output.AppendLine(
                "REVISED PRIMAL SIMPLEX ALGORITHM");

            output.AppendLine(
                "============================================");

            output.AppendLine();

            DisplayCanonicalForm(
                output,
                objective,
                constraints,
                rhs,
                isMaximization);

            int iteration = 1;

            while (iteration <= 1000)
            {
                output.AppendLine();
                output.AppendLine(
                    "============================================");

                output.AppendLine(
                    "ITERATION T-" + iteration);

                output.AppendLine(
                    "============================================");

                // --------------------------------------------
                // Construct current basis B
                // --------------------------------------------
                double[,] B =
                    GetBasisMatrix(
                        fullA,
                        basis);

                double[,] BInverse =
                    InvertMatrix(B);

                double[] xB =
                    Multiply(
                        BInverse,
                        rhs);

                for (int i = 0;
                     i < xB.Length;
                     i++)
                {
                    if (Math.Abs(xB[i]) < EPS)
                        xB[i] = 0;
                }

                double[] cB =
                    new double[numberOfConstraints];

                for (int i = 0;
                     i < numberOfConstraints;
                     i++)
                {
                    cB[i] =
                        costs[basis[i]];
                }

                // y = Cbv B^-1
                double[] y =
                    MultiplyRowVector(
                        cB,
                        BInverse);

                // --------------------------------------------
                // PRODUCT FORM
                // --------------------------------------------
                output.AppendLine();
                output.AppendLine("PRODUCT FORM");
                output.AppendLine("--------------------------------------------");

                output.Append("Xbv = ");

                for (int i = 0; i < basis.Length; i++)
                {
                    output.Append(
                        VariableName(
                            basis[i],
                            numberOfVariables));

                    if (i < basis.Length - 1)
                        output.Append(", ");
                }

                output.AppendLine();

                List<int> nonBasic =
                    GetNonBasicVariables(
                        totalVariables,
                        basis);

                output.Append("Xnbv = ");

                for (int i = 0;
                     i < nonBasic.Count;
                     i++)
                {
                    output.Append(
                        VariableName(
                            nonBasic[i],
                            numberOfVariables));

                    if (i < nonBasic.Count - 1)
                        output.Append(", ");
                }

                output.AppendLine();

                output.Append("Cbv = ");

                for (int i = 0;
                     i < cB.Length;
                     i++)
                {
                    output.Append(F(cB[i]));

                    if (i < cB.Length - 1)
                        output.Append("  ");
                }

                output.AppendLine();

                output.AppendLine();
                output.AppendLine("B:");

                AppendMatrix(
                    output,
                    B);

                output.AppendLine();
                output.AppendLine("B^-1:");

                AppendMatrix(
                    output,
                    BInverse);

                output.AppendLine();
                output.Append("CbvB^-1 = ");

                for (int i = 0;
                     i < y.Length;
                     i++)
                {
                    output.Append(F(y[i]));

                    if (i < y.Length - 1)
                        output.Append("  ");
                }

                output.AppendLine();

                output.AppendLine();
                output.AppendLine(
                    "b* = B^-1 b:");

                AppendVector(
                    output,
                    xB);

                // --------------------------------------------
                // PRICE OUT
                // --------------------------------------------
                output.AppendLine();
                output.AppendLine("PRICE OUT");
                output.AppendLine("--------------------------------------------");

                int enteringVariable = -1;
                double mostNegativePriceOut = 0;

                foreach (int variable in nonBasic)
                {
                    double[] column =
                        GetColumn(
                            fullA,
                            variable);

                    // Price Out used in the course:
                    // Cbv B^-1 Aj - Cj
                    double priceOut =
                        Dot(y, column) -
                        costs[variable];

                    if (Math.Abs(priceOut) < EPS)
                        priceOut = 0;

                    output.AppendLine(
                        VariableName(
                            variable,
                            numberOfVariables) +
                        " = " +
                        F(priceOut));

                    if (priceOut <
                        mostNegativePriceOut - EPS)
                    {
                        mostNegativePriceOut =
                            priceOut;

                        enteringVariable =
                            variable;
                    }
                }

                // --------------------------------------------
                // OPTIMAL
                // --------------------------------------------
                if (enteringVariable == -1)
                {
                    output.AppendLine();
                    output.AppendLine(
                        "All Price Out values are >= 0.");

                    output.AppendLine(
                        "Therefore the solution is optimal.");

                    double[] fullSolution =
                        new double[totalVariables];

                    for (int row = 0;
                         row < basis.Length;
                         row++)
                    {
                        fullSolution[basis[row]] =
                            xB[row];
                    }

                    LastSolution =
                        new double[numberOfVariables];

                    for (int i = 0;
                         i < numberOfVariables;
                         i++)
                    {
                        LastSolution[i] =
                            Math.Abs(fullSolution[i]) < EPS
                                ? 0
                                : fullSolution[i];
                    }

                    LastObjectiveValue =
                        Dot(
                            objective,
                            LastSolution);

                    output.AppendLine();
                    output.AppendLine(
                        "============================================");

                    output.AppendLine(
                        "OPTIMAL SOLUTION");

                    output.AppendLine(
                        "============================================");

                    for (int i = 0;
                         i < LastSolution.Length;
                         i++)
                    {
                        output.AppendLine(
                            "x" +
                            (i + 1) +
                            " = " +
                            F(LastSolution[i]));
                    }

                    output.AppendLine(
                        "optimal z = " +
                        F(LastObjectiveValue));

                    break;
                }

                output.AppendLine();
                output.AppendLine(
                    "Entering variable = " +
                    VariableName(
                        enteringVariable,
                        numberOfVariables));

                // --------------------------------------------
                // Aj* = B^-1 Aj
                // --------------------------------------------
                double[] enteringColumn =
                    GetColumn(
                        fullA,
                        enteringVariable);

                double[] transformedColumn =
                    Multiply(
                        BInverse,
                        enteringColumn);

                output.AppendLine();
                output.AppendLine(
                    "A*j = B^-1 Aj:");

                AppendVector(
                    output,
                    transformedColumn);

                // --------------------------------------------
                // RATIO TEST
                // --------------------------------------------
                output.AppendLine();
                output.AppendLine("RATIO TEST");
                output.AppendLine("--------------------------------------------");

                int leavingRow = -1;
                double minimumRatio =
                    double.PositiveInfinity;

                for (int row = 0;
                     row < numberOfConstraints;
                     row++)
                {
                    if (transformedColumn[row] > EPS)
                    {
                        double ratio =
                            xB[row] /
                            transformedColumn[row];

                        output.AppendLine(
                            VariableName(
                                basis[row],
                                numberOfVariables) +
                            ": " +
                            F(xB[row]) +
                            " / " +
                            F(transformedColumn[row]) +
                            " = " +
                            F(ratio));

                        if (ratio <
                            minimumRatio - EPS)
                        {
                            minimumRatio = ratio;
                            leavingRow = row;
                        }
                    }
                    else
                    {
                        output.AppendLine(
                            VariableName(
                                basis[row],
                                numberOfVariables) +
                            ": n/a");
                    }
                }

                if (leavingRow == -1)
                {
                    throw new InvalidOperationException(
                        "Programming model is unbounded.");
                }

                int leavingVariable =
                    basis[leavingRow];

                output.AppendLine();
                output.AppendLine(
                    "Leaving variable = " +
                    VariableName(
                        leavingVariable,
                        numberOfVariables));

                output.AppendLine(
                    "Pivot row = " +
                    (leavingRow + 1));

                output.AppendLine(
                    "theta = " +
                    F(minimumRatio));

                // --------------------------------------------
                // Update basis
                // --------------------------------------------
                basis[leavingRow] =
                    enteringVariable;

                iteration++;
            }

            if (iteration > 1000)
            {
                throw new InvalidOperationException(
                    "Maximum simplex iterations exceeded.");
            }

            string result =
                output.ToString();

            Console.WriteLine(result);

            return result;
        }

        private void DisplayCanonicalForm(
            StringBuilder output,
            double[] objective,
            double[,] constraints,
            double[] rhs,
            bool isMaximization)
        {
            int n =
                objective.Length;

            int m =
                rhs.Length;

            output.AppendLine("CANONICAL FORM");
            output.AppendLine("--------------------------------------------");

            output.Append(
                isMaximization
                    ? "max z = "
                    : "min z = ");

            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    output.Append(
                        objective[i] >= 0
                            ? " + "
                            : " - ");
                }
                else if (objective[i] < 0)
                {
                    output.Append("-");
                }

                output.Append(
                    F(Math.Abs(objective[i])) +
                    "x" + (i + 1));
            }

            output.AppendLine();

            output.Append("z ");

            for (int i = 0; i < n; i++)
            {
                if (objective[i] >= 0)
                    output.Append("- ");
                else
                    output.Append("+ ");

                output.Append(
                    F(Math.Abs(objective[i])) +
                    "x" + (i + 1) + " ");
            }

            output.AppendLine("= 0");

            for (int row = 0; row < m; row++)
            {
                for (int col = 0; col < n; col++)
                {
                    double value =
                        constraints[row, col];

                    if (col > 0)
                    {
                        output.Append(
                            value >= 0
                                ? " + "
                                : " - ");
                    }
                    else if (value < 0)
                    {
                        output.Append("-");
                    }

                    output.Append(
                        F(Math.Abs(value)) +
                        "x" + (col + 1));
                }

                output.Append(
                    " + s" +
                    (row + 1));

                output.AppendLine(
                    " = " +
                    F(rhs[row]));
            }
        }

        private double[,] GetBasisMatrix(
            double[,] matrix,
            int[] basis)
        {
            int rows =
                matrix.GetLength(0);

            double[,] result =
                new double[rows, rows];

            for (int col = 0;
                 col < basis.Length;
                 col++)
            {
                for (int row = 0;
                     row < rows;
                     row++)
                {
                    result[row, col] =
                        matrix[
                            row,
                            basis[col]];
                }
            }

            return result;
        }

        private List<int> GetNonBasicVariables(
            int totalVariables,
            int[] basis)
        {
            HashSet<int> basic =
                new HashSet<int>(basis);

            List<int> result =
                new List<int>();

            for (int i = 0;
                 i < totalVariables;
                 i++)
            {
                if (!basic.Contains(i))
                    result.Add(i);
            }

            return result;
        }

        private double[] GetColumn(
            double[,] matrix,
            int column)
        {
            int rows =
                matrix.GetLength(0);

            double[] result =
                new double[rows];

            for (int row = 0;
                 row < rows;
                 row++)
            {
                result[row] =
                    matrix[row, column];
            }

            return result;
        }

        private double[] Multiply(
            double[,] matrix,
            double[] vector)
        {
            int rows =
                matrix.GetLength(0);

            int cols =
                matrix.GetLength(1);

            if (cols != vector.Length)
                throw new ArgumentException(
                    "Matrix/vector dimensions do not match.");

            double[] result =
                new double[rows];

            for (int row = 0;
                 row < rows;
                 row++)
            {
                double total = 0;

                for (int col = 0;
                     col < cols;
                     col++)
                {
                    total +=
                        matrix[row, col] *
                        vector[col];
                }

                result[row] = total;
            }

            return result;
        }

        private double[] MultiplyRowVector(
            double[] vector,
            double[,] matrix)
        {
            int rows =
                matrix.GetLength(0);

            int cols =
                matrix.GetLength(1);

            if (vector.Length != rows)
                throw new ArgumentException(
                    "Vector/matrix dimensions do not match.");

            double[] result =
                new double[cols];

            for (int col = 0;
                 col < cols;
                 col++)
            {
                double total = 0;

                for (int row = 0;
                     row < rows;
                     row++)
                {
                    total +=
                        vector[row] *
                        matrix[row, col];
                }

                result[col] = total;
            }

            return result;
        }

        private double Dot(
            double[] a,
            double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException(
                    "Vector dimensions do not match.");

            double result = 0;

            for (int i = 0;
                 i < a.Length;
                 i++)
            {
                result +=
                    a[i] * b[i];
            }

            return result;
        }

        private double[,] InvertMatrix(
            double[,] matrix)
        {
            int n =
                matrix.GetLength(0);

            if (n != matrix.GetLength(1))
                throw new ArgumentException(
                    "Only square matrices can be inverted.");

            double[,] augmented =
                new double[n, n * 2];

            for (int row = 0;
                 row < n;
                 row++)
            {
                for (int col = 0;
                     col < n;
                     col++)
                {
                    augmented[row, col] =
                        matrix[row, col];
                }

                augmented[row, n + row] = 1;
            }

            for (int pivot = 0;
                 pivot < n;
                 pivot++)
            {
                int bestRow = pivot;

                for (int row = pivot + 1;
                     row < n;
                     row++)
                {
                    if (Math.Abs(
                            augmented[row, pivot]) >
                        Math.Abs(
                            augmented[bestRow, pivot]))
                    {
                        bestRow = row;
                    }
                }

                if (Math.Abs(
                        augmented[bestRow, pivot]) <
                    EPS)
                {
                    throw new InvalidOperationException(
                        "Basis matrix is singular.");
                }

                if (bestRow != pivot)
                {
                    SwapRows(
                        augmented,
                        bestRow,
                        pivot);
                }

                double pivotValue =
                    augmented[pivot, pivot];

                for (int col = 0;
                     col < n * 2;
                     col++)
                {
                    augmented[pivot, col] /=
                        pivotValue;
                }

                for (int row = 0;
                     row < n;
                     row++)
                {
                    if (row == pivot)
                        continue;

                    double multiplier =
                        augmented[row, pivot];

                    for (int col = 0;
                         col < n * 2;
                         col++)
                    {
                        augmented[row, col] -=
                            multiplier *
                            augmented[pivot, col];
                    }
                }
            }

            double[,] inverse =
                new double[n, n];

            for (int row = 0;
                 row < n;
                 row++)
            {
                for (int col = 0;
                     col < n;
                     col++)
                {
                    inverse[row, col] =
                        augmented[row, n + col];

                    if (Math.Abs(
                            inverse[row, col]) < EPS)
                    {
                        inverse[row, col] = 0;
                    }
                }
            }

            return inverse;
        }

        private void SwapRows(
            double[,] matrix,
            int rowA,
            int rowB)
        {
            int cols =
                matrix.GetLength(1);

            for (int col = 0;
                 col < cols;
                 col++)
            {
                double temp =
                    matrix[rowA, col];

                matrix[rowA, col] =
                    matrix[rowB, col];

                matrix[rowB, col] =
                    temp;
            }
        }

        private void AppendMatrix(
            StringBuilder output,
            double[,] matrix)
        {
            int rows =
                matrix.GetLength(0);

            int cols =
                matrix.GetLength(1);

            for (int row = 0;
                 row < rows;
                 row++)
            {
                for (int col = 0;
                     col < cols;
                     col++)
                {
                    output.Append(
                        F(matrix[row, col]));

                    if (col < cols - 1)
                        output.Append("\t");
                }

                output.AppendLine();
            }
        }

        private void AppendVector(
            StringBuilder output,
            double[] vector)
        {
            for (int i = 0;
                 i < vector.Length;
                 i++)
            {
                output.AppendLine(
                    F(vector[i]));
            }
        }

        private string VariableName(
            int index,
            int originalVariableCount)
        {
            if (index < originalVariableCount)
            {
                return "x" +
                    (index + 1);
            }

            return "s" +
                (index -
                 originalVariableCount +
                 1);
        }

        private void ValidateInput(
            double[] objective,
            double[,] constraints,
            double[] rhs)
        {
            if (objective == null ||
                constraints == null ||
                rhs == null)
            {
                throw new ArgumentException(
                    "Model data cannot be null.");
            }

            if (objective.Length == 0)
            {
                throw new ArgumentException(
                    "At least one decision variable is required.");
            }

            if (rhs.Length == 0)
            {
                throw new ArgumentException(
                    "At least one constraint is required.");
            }

            if (constraints.GetLength(0) !=
                rhs.Length)
            {
                throw new ArgumentException(
                    "Constraint rows must match RHS values.");
            }

            if (constraints.GetLength(1) !=
                objective.Length)
            {
                throw new ArgumentException(
                    "Constraint columns must match decision variables.");
            }
        }

        private string F(double value)
        {
            if (Math.Abs(value) < EPS)
                value = 0;

            return Math.Round(value, 3)
                .ToString("0.000");
        }
    }
}