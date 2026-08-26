using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LPR381Proj.Models;

namespace LPR381Proj.Algorithms
{
    internal class RevisedSimplexSolver
    {
        private const double EPS = 0.0000001;
        private const int MAX_ITERATIONS = 1000;

        public double[] LastSolution { get; private set; }
        public double LastObjectiveValue { get; private set; }

        public string Solve(LinearProgram model)
        {
            ValidateModel(model);

            int n = model.VariableCount;
            int m = model.Constraints.Count;
            int totalVariables = n + m;

            double[,] A =
                new double[m, totalVariables];

            double[] b = new double[m];
            double[] c = new double[totalVariables];

            // Convert minimization to equivalent maximization.
            for (int j = 0; j < n; j++)
            {
                c[j] =
                    model.Objective ==
                    ObjectiveType.Maximize
                        ? model.ObjectiveCoefficients[j]
                        : -model.ObjectiveCoefficients[j];
            }

            // Construct A with slack variables.
            for (int i = 0; i < m; i++)
            {
                Constraint constraint =
                    model.Constraints[i];

                for (int j = 0; j < n; j++)
                {
                    A[i, j] =
                        constraint.Coefficients[j];
                }

                A[i, n + i] = 1.0;
                b[i] = constraint.RHS;
            }

            // Initial basis = slack variables.
            int[] basis = new int[m];

            for (int i = 0; i < m; i++)
                basis[i] = n + i;

            StringBuilder output =
                new StringBuilder();

            PrintCanonicalForm(
                output,
                model);

            bool optimal = false;
            bool unbounded = false;

            int iteration = 1;

            while (iteration <= MAX_ITERATIONS)
            {
                double[,] B =
                    BuildBasisMatrix(A, basis);

                double[,] inverseB =
                    InvertMatrix(B);

                double[] xB =
                    Multiply(
                        inverseB,
                        b);

                double[] cB =
                    new double[m];

                for (int i = 0; i < m; i++)
                {
                    cB[i] = c[basis[i]];
                }

                double[] priceVector =
                    MultiplyRowVector(
                        cB,
                        inverseB);

                List<int> nonBasic =
                    GetNonBasicVariables(
                        totalVariables,
                        basis);

                PrintIterationHeader(
                    output,
                    iteration);

                PrintProductForm(
                    output,
                    basis,
                    nonBasic,
                    cB,
                    B,
                    inverseB,
                    priceVector,
                    xB,
                    n);

                output.AppendLine();
                output.AppendLine("PRICE OUT");
                output.AppendLine(
                    "--------------------------------------------");

                int enteringVariable = -1;
                double mostNegative = -EPS;

                foreach (int variable in nonBasic)
                {
                    double[] column =
                        GetColumn(A, variable);

                    double priceOut =
                        Dot(priceVector, column) -
                        c[variable];

                    output.AppendLine(
                        VariableName(variable, n) +
                        " = " +
                        priceOut.ToString("0.000"));

                    if (priceOut < mostNegative)
                    {
                        mostNegative = priceOut;
                        enteringVariable = variable;
                    }
                }

                if (enteringVariable == -1)
                {
                    output.AppendLine();
                    output.AppendLine(
                        "All Price Out values are >= 0.");

                    output.AppendLine(
                        "Therefore the solution is optimal.");

                    optimal = true;
                    break;
                }

                output.AppendLine();

                output.AppendLine(
                    "Entering variable = " +
                    VariableName(
                        enteringVariable,
                        n));

                double[] enteringColumn =
                    GetColumn(
                        A,
                        enteringVariable);

                double[] transformedColumn =
                    Multiply(
                        inverseB,
                        enteringColumn);

                output.AppendLine();
                output.AppendLine(
                    "A*j = B^-1 Aj:");

                foreach (double value in transformedColumn)
                {
                    output.AppendLine(
                        value.ToString("0.000"));
                }

                output.AppendLine();
                output.AppendLine("RATIO TEST");
                output.AppendLine(
                    "--------------------------------------------");

                int leavingRow = -1;
                double minimumRatio =
                    double.PositiveInfinity;

                for (int i = 0; i < m; i++)
                {
                    string basicName =
                        VariableName(
                            basis[i],
                            n);

                    if (transformedColumn[i] > EPS)
                    {
                        double ratio =
                            xB[i] /
                            transformedColumn[i];

                        output.AppendLine(
                            basicName +
                            ": " +
                            xB[i].ToString("0.000") +
                            " / " +
                            transformedColumn[i]
                                .ToString("0.000") +
                            " = " +
                            ratio.ToString("0.000"));

                        if (ratio <
                            minimumRatio - EPS)
                        {
                            minimumRatio = ratio;
                            leavingRow = i;
                        }
                    }
                    else
                    {
                        output.AppendLine(
                            basicName +
                            ": n/a");
                    }
                }

                if (leavingRow == -1)
                {
                    output.AppendLine();
                    output.AppendLine(
                        "No valid leaving variable.");

                    output.AppendLine(
                        "The problem is unbounded.");

                    unbounded = true;
                    break;
                }

                output.AppendLine();

                output.AppendLine(
                    "Leaving variable = " +
                    VariableName(
                        basis[leavingRow],
                        n));

                output.AppendLine(
                    "Pivot row = " +
                    (leavingRow + 1));

                output.AppendLine(
                    "theta = " +
                    minimumRatio.ToString("0.000"));

                basis[leavingRow] =
                    enteringVariable;

                output.AppendLine();

                iteration++;
            }

            if (!optimal && !unbounded &&
                iteration > MAX_ITERATIONS)
            {
                output.AppendLine();
                output.AppendLine(
                    "ITERATION LIMIT REACHED");

                return output.ToString();
            }

            if (unbounded)
                return output.ToString();

            double[,] finalB =
                BuildBasisMatrix(A, basis);

            double[,] finalInverse =
                InvertMatrix(finalB);

            double[] finalXB =
                Multiply(finalInverse, b);

            double[] solution =
                new double[n];

            for (int i = 0; i < basis.Length; i++)
            {
                int variable = basis[i];

                if (variable < n)
                {
                    solution[variable] =
                        Math.Abs(finalXB[i]) < EPS
                            ? 0.0
                            : finalXB[i];
                }
            }

            double objectiveValue = 0.0;

            for (int j = 0; j < n; j++)
            {
                objectiveValue +=
                    model.ObjectiveCoefficients[j] *
                    solution[j];
            }

            LastSolution = solution;
            LastObjectiveValue =
                objectiveValue;

            output.AppendLine();
            output.AppendLine(
                "============================================");

            output.AppendLine(
                "OPTIMAL SOLUTION");

            output.AppendLine(
                "============================================");

            for (int i = 0; i < solution.Length; i++)
            {
                output.AppendLine(
                    "x" +
                    (i + 1) +
                    " = " +
                    solution[i].ToString("0.000"));
            }

            output.AppendLine(
                "optimal z = " +
                objectiveValue.ToString("0.000"));

            return output.ToString();
        }

        private void ValidateModel(
            LinearProgram model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (model.VariableCount == 0)
            {
                throw new InvalidOperationException(
                    "The problem contains no decision variables.");
            }

            if (model.Constraints.Count == 0)
            {
                throw new InvalidOperationException(
                    "The problem contains no constraints.");
            }

            foreach (Constraint constraint
                in model.Constraints)
            {
                if (constraint.Relation !=
                    RelationType.LessThanOrEqual)
                {
                    throw new InvalidOperationException(
                        "Revised Primal Simplex currently requires <= constraints.");
                }

                if (constraint.RHS < -EPS)
                {
                    throw new InvalidOperationException(
                        "Revised Primal Simplex requires a non-negative RHS for the initial feasible basis.");
                }

                if (constraint.Coefficients.Count !=
                    model.VariableCount)
                {
                    throw new InvalidOperationException(
                        "Constraint coefficient count does not match variable count.");
                }
            }

            if (model.VariableTypes.Count ==
                model.VariableCount)
            {
                foreach (VariableType type
                    in model.VariableTypes)
                {
                    if (type !=
                        VariableType.Continuous)
                    {
                        throw new InvalidOperationException(
                            "Revised Primal Simplex requires continuous non-negative variables.");
                    }
                }
            }
        }

        private void PrintCanonicalForm(
            StringBuilder output,
            LinearProgram model)
        {
            output.AppendLine(
                "============================================");

            output.AppendLine(
                "REVISED PRIMAL SIMPLEX ALGORITHM");

            output.AppendLine(
                "============================================");

            output.AppendLine();

            output.AppendLine(
                "CANONICAL FORM");

            output.AppendLine(
                "--------------------------------------------");

            string objectiveName =
                model.Objective ==
                ObjectiveType.Maximize
                    ? "max"
                    : "min";

            output.AppendLine(
                objectiveName +
                " z = " +
                BuildExpression(
                    model.ObjectiveCoefficients));

            StringBuilder objectiveEquation =
                new StringBuilder("z");

            for (int i = 0;
                i < model.VariableCount;
                i++)
            {
                double coefficient =
                    model.ObjectiveCoefficients[i];

                if (coefficient >= 0)
                {
                    objectiveEquation.Append(
                        " - " +
                        Math.Abs(coefficient)
                            .ToString("0.000") +
                        "x" +
                        (i + 1));
                }
                else
                {
                    objectiveEquation.Append(
                        " + " +
                        Math.Abs(coefficient)
                            .ToString("0.000") +
                        "x" +
                        (i + 1));
                }
            }

            objectiveEquation.Append(" = 0");

            output.AppendLine(
                objectiveEquation.ToString());

            for (int i = 0;
                i < model.Constraints.Count;
                i++)
            {
                output.AppendLine(
                    BuildExpression(
                        model.Constraints[i]
                            .Coefficients) +
                    " + s" +
                    (i + 1) +
                    " = " +
                    model.Constraints[i]
                        .RHS
                        .ToString("0.000"));
            }

            output.AppendLine();
        }

        private void PrintIterationHeader(
            StringBuilder output,
            int iteration)
        {
            output.AppendLine(
                "============================================");

            output.AppendLine(
                "ITERATION T-" +
                iteration);

            output.AppendLine(
                "============================================");

            output.AppendLine();
        }

        private void PrintProductForm(
            StringBuilder output,
            int[] basis,
            List<int> nonBasic,
            double[] cB,
            double[,] B,
            double[,] inverseB,
            double[] priceVector,
            double[] xB,
            int originalVariableCount)
        {
            output.AppendLine(
                "PRODUCT FORM");

            output.AppendLine(
                "--------------------------------------------");

            output.AppendLine(
                "Xbv = " +
                string.Join(
                    ", ",
                    basis.Select(
                        x => VariableName(
                            x,
                            originalVariableCount))));

            output.AppendLine(
                "Xnbv = " +
                string.Join(
                    ", ",
                    nonBasic.Select(
                        x => VariableName(
                            x,
                            originalVariableCount))));

            output.AppendLine(
                "Cbv = " +
                string.Join(
                    "  ",
                    cB.Select(
                        x => x.ToString("0.000"))));

            output.AppendLine();
            output.AppendLine("B:");

            PrintMatrix(output, B);

            output.AppendLine();
            output.AppendLine("B^-1:");

            PrintMatrix(
                output,
                inverseB);

            output.AppendLine();

            output.AppendLine(
                "CbvB^-1 = " +
                string.Join(
                    "  ",
                    priceVector.Select(
                        x => x.ToString("0.000"))));

            output.AppendLine();
            output.AppendLine(
                "b* = B^-1 b:");

            foreach (double value in xB)
            {
                output.AppendLine(
                    value.ToString("0.000"));
            }
        }

        private double[,] BuildBasisMatrix(
            double[,] A,
            int[] basis)
        {
            int rows = A.GetLength(0);

            double[,] B =
                new double[rows, rows];

            for (int col = 0;
                col < rows;
                col++)
            {
                for (int row = 0;
                    row < rows;
                    row++)
                {
                    B[row, col] =
                        A[row, basis[col]];
                }
            }

            return B;
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
            int rows = matrix.GetLength(0);

            double[] result =
                new double[rows];

            for (int i = 0; i < rows; i++)
            {
                result[i] =
                    matrix[i, column];
            }

            return result;
        }

        private double[] Multiply(
            double[,] matrix,
            double[] vector)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[] result =
                new double[rows];

            for (int i = 0; i < rows; i++)
            {
                double sum = 0;

                for (int j = 0; j < cols; j++)
                {
                    sum +=
                        matrix[i, j] *
                        vector[j];
                }

                result[i] = sum;
            }

            return result;
        }

        private double[] MultiplyRowVector(
            double[] vector,
            double[,] matrix)
        {
            int cols = matrix.GetLength(1);

            double[] result =
                new double[cols];

            for (int j = 0; j < cols; j++)
            {
                double sum = 0;

                for (int i = 0;
                    i < vector.Length;
                    i++)
                {
                    sum +=
                        vector[i] *
                        matrix[i, j];
                }

                result[j] = sum;
            }

            return result;
        }

        private double Dot(
            double[] a,
            double[] b)
        {
            double sum = 0;

            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }

            return sum;
        }

        private double[,] InvertMatrix(
            double[,] matrix)
        {
            int n = matrix.GetLength(0);

            double[,] augmented =
                new double[n, n * 2];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] =
                        matrix[i, j];
                }

                augmented[i, n + i] = 1.0;
            }

            for (int col = 0; col < n; col++)
            {
                int pivotRow = col;
                double largest =
                    Math.Abs(
                        augmented[pivotRow, col]);

                for (int row = col + 1;
                    row < n;
                    row++)
                {
                    double candidate =
                        Math.Abs(
                            augmented[row, col]);

                    if (candidate > largest)
                    {
                        largest = candidate;
                        pivotRow = row;
                    }
                }

                if (largest < EPS)
                {
                    throw new InvalidOperationException(
                        "Basis matrix is singular and cannot be inverted.");
                }

                if (pivotRow != col)
                {
                    for (int j = 0;
                        j < n * 2;
                        j++)
                    {
                        double temp =
                            augmented[col, j];

                        augmented[col, j] =
                            augmented[pivotRow, j];

                        augmented[pivotRow, j] =
                            temp;
                    }
                }

                double pivot =
                    augmented[col, col];

                for (int j = 0;
                    j < n * 2;
                    j++)
                {
                    augmented[col, j] /= pivot;
                }

                for (int row = 0;
                    row < n;
                    row++)
                {
                    if (row == col)
                        continue;

                    double factor =
                        augmented[row, col];

                    for (int j = 0;
                        j < n * 2;
                        j++)
                    {
                        augmented[row, j] -=
                            factor *
                            augmented[col, j];
                    }
                }
            }

            double[,] inverse =
                new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    inverse[i, j] =
                        augmented[i, n + j];
                }
            }

            return inverse;
        }

        private void PrintMatrix(
            StringBuilder output,
            double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                StringBuilder row =
                    new StringBuilder();

                for (int j = 0; j < cols; j++)
                {
                    if (j > 0)
                        row.Append("   ");

                    row.Append(
                        matrix[i, j]
                            .ToString("0.000"));
                }

                output.AppendLine(
                    row.ToString());
            }
        }

        private string VariableName(
            int index,
            int originalVariableCount)
        {
            if (index <
                originalVariableCount)
            {
                return "x" +
                    (index + 1);
            }

            return "s" +
                (index -
                originalVariableCount + 1);
        }

        private string BuildExpression(
            IList<double> coefficients)
        {
            StringBuilder sb =
                new StringBuilder();

            for (int i = 0;
                i < coefficients.Count;
                i++)
            {
                double coefficient =
                    coefficients[i];

                if (i > 0)
                {
                    sb.Append(
                        coefficient >= 0
                            ? " + "
                            : " - ");
                }
                else if (coefficient < 0)
                {
                    sb.Append("-");
                }

                sb.Append(
                    Math.Abs(coefficient)
                        .ToString("0.000"));

                sb.Append("x");
                sb.Append(i + 1);
            }

            return sb.ToString();
        }
    }
}