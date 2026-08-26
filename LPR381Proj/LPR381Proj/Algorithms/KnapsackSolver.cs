using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LPR381Proj.Models;

namespace LPR381Proj.Algorithms
{
    internal class KnapsackSolver
    {
        private const double EPS = 0.0000001;

        private class Item
        {
            public int OriginalIndex { get; set; }
            public double Profit { get; set; }
            public double Weight { get; set; }
            public double Ratio { get; set; }
        }

        private List<Item> items;
        private double capacity;
        private double bestProfit;
        private double bestWeight;
        private int[] bestDecision;
        private int nodeCounter;
        private StringBuilder output;

        public int[] LastSolution { get; private set; }
        public double LastObjectiveValue { get; private set; }
        public double LastWeight { get; private set; }

        public string Solve(IntegerProgram model)
        {
            ValidateModel(model);

            int n = model.VariableCount;

            capacity = model.Constraints[0].RHS;
            bestProfit = 0.0;
            bestWeight = 0.0;
            bestDecision = new int[n];
            nodeCounter = 0;
            output = new StringBuilder();

            items = new List<Item>();

            for (int i = 0; i < n; i++)
            {
                double profit = model.ObjectiveCoefficients[i];
                double weight = model.Constraints[0].Coefficients[i];

                double ratio;

                if (Math.Abs(weight) < EPS)
                {
                    ratio = profit > 0
                        ? double.PositiveInfinity
                        : 0.0;
                }
                else
                {
                    ratio = profit / weight;
                }

                items.Add(new Item
                {
                    OriginalIndex = i,
                    Profit = profit,
                    Weight = weight,
                    Ratio = ratio
                });
            }

            items = items
                .OrderByDescending(x => x.Ratio)
                .ThenBy(x => x.OriginalIndex)
                .ToList();

            PrintModel(model);
            PrintItemOrder();

            output.AppendLine();
            output.AppendLine("SEARCH TREE");
            output.AppendLine("========================================");

            int[] decisions = Enumerable.Repeat(-1, n).ToArray();

            Explore(
                level: 0,
                currentWeight: 0.0,
                currentProfit: 0.0,
                decisions: decisions);

            LastSolution = (int[])bestDecision.Clone();
            LastObjectiveValue = bestProfit;
            LastWeight = bestWeight;

            PrintBestCandidate();

            return output.ToString();
        }

        private void Explore(
            int level,
            double currentWeight,
            double currentProfit,
            int[] decisions)
        {
            nodeCounter++;

            double upperBound =
                currentWeight > capacity + EPS
                    ? double.NegativeInfinity
                    : CalculateUpperBound(
                        level,
                        currentWeight,
                        currentProfit);

            PrintNode(
                nodeCounter,
                level,
                currentWeight,
                currentProfit,
                upperBound,
                decisions);

            if (currentWeight > capacity + EPS)
            {
                output.AppendLine(
                    "status: fathomed - infeasible");

                output.AppendLine();
                return;
            }

            if (currentProfit > bestProfit + EPS)
            {
                bestProfit = currentProfit;
                bestWeight = currentWeight;

                for (int i = 0; i < decisions.Length; i++)
                {
                    bestDecision[i] =
                        decisions[i] == 1 ? 1 : 0;
                }

                output.AppendLine(
                    "status: new candidate");

                output.AppendLine(
                    "candidate z = " +
                    bestProfit.ToString("0.000"));
            }

            if (level >= items.Count)
            {
                output.AppendLine(
                    "status: fathomed - complete integer solution");

                output.AppendLine();
                return;
            }

            if (upperBound <= bestProfit + EPS)
            {
                output.AppendLine(
                    "status: fathomed - bound");

                output.AppendLine(
                    "upper bound cannot improve best candidate");

                output.AppendLine();
                return;
            }

            Item currentItem = items[level];

            output.AppendLine(
                "branching variable: x" +
                (currentItem.OriginalIndex + 1));

            output.AppendLine();

            // Branch 1: xi = 1
            decisions[currentItem.OriginalIndex] = 1;

            output.AppendLine(
                "sub-problem: x" +
                (currentItem.OriginalIndex + 1) +
                " = 1");

            Explore(
                level + 1,
                currentWeight + currentItem.Weight,
                currentProfit + currentItem.Profit,
                decisions);

            output.AppendLine(
                "backtrack from x" +
                (currentItem.OriginalIndex + 1) +
                " = 1");

            // Branch 2: xi = 0
            decisions[currentItem.OriginalIndex] = 0;

            output.AppendLine(
                "sub-problem: x" +
                (currentItem.OriginalIndex + 1) +
                " = 0");

            Explore(
                level + 1,
                currentWeight,
                currentProfit,
                decisions);

            output.AppendLine(
                "backtrack from x" +
                (currentItem.OriginalIndex + 1) +
                " = 0");

            decisions[currentItem.OriginalIndex] = -1;
        }

        private double CalculateUpperBound(
            int level,
            double currentWeight,
            double currentProfit)
        {
            if (currentWeight > capacity + EPS)
                return double.NegativeInfinity;

            double bound = currentProfit;
            double weightUsed = currentWeight;

            for (int i = level; i < items.Count; i++)
            {
                Item item = items[i];

                if (item.Profit <= 0)
                    continue;

                if (Math.Abs(item.Weight) < EPS)
                {
                    bound += item.Profit;
                    continue;
                }

                if (weightUsed + item.Weight <= capacity + EPS)
                {
                    weightUsed += item.Weight;
                    bound += item.Profit;
                }
                else
                {
                    double remaining =
                        capacity - weightUsed;

                    if (remaining > EPS)
                    {
                        bound +=
                            item.Profit *
                            (remaining / item.Weight);
                    }

                    break;
                }
            }

            return bound;
        }

        private void ValidateModel(IntegerProgram model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (model.Objective != ObjectiveType.Maximize)
            {
                throw new InvalidOperationException(
                    "Branch & Bound Knapsack requires a maximization problem.");
            }

            if (model.VariableCount == 0)
            {
                throw new InvalidOperationException(
                    "The problem contains no decision variables.");
            }

            if (model.Constraints.Count != 1)
            {
                throw new InvalidOperationException(
                    "Branch & Bound Knapsack requires exactly one capacity constraint.");
            }

            Constraint constraint = model.Constraints[0];

            if (constraint.Relation !=
                RelationType.LessThanOrEqual)
            {
                throw new InvalidOperationException(
                    "The Knapsack capacity constraint must use <=.");
            }

            if (constraint.Coefficients.Count !=
                model.VariableCount)
            {
                throw new InvalidOperationException(
                    "Constraint coefficient count does not match variable count.");
            }

            if (model.VariableTypes.Count !=
                model.VariableCount ||
                model.VariableTypes.Any(
                    x => x != VariableType.Binary))
            {
                throw new InvalidOperationException(
                    "Branch & Bound Knapsack requires all variables to be binary.");
            }

            if (constraint.RHS < 0)
            {
                throw new InvalidOperationException(
                    "Knapsack capacity cannot be negative.");
            }

            if (constraint.Coefficients.Any(x => x < 0))
            {
                throw new InvalidOperationException(
                    "Knapsack item weights cannot be negative.");
            }
        }

        private void PrintModel(IntegerProgram model)
        {
            output.AppendLine(
                "========================================");

            output.AppendLine(
                "BRANCH AND BOUND KNAPSACK");

            output.AppendLine(
                "========================================");

            output.AppendLine();

            output.AppendLine(
                "max z = " +
                BuildExpression(
                    model.ObjectiveCoefficients));

            output.AppendLine(
                "s.t. " +
                BuildExpression(
                    model.Constraints[0].Coefficients) +
                " <= " +
                capacity.ToString("0.000"));

            output.AppendLine(
                "x1 ... x" +
                model.VariableCount +
                " binary");

            output.AppendLine();
        }

        private void PrintItemOrder()
        {
            output.AppendLine("ITEM ORDER");
            output.AppendLine("----------------------------------------");

            foreach (Item item in items)
            {
                string ratio =
                    double.IsPositiveInfinity(item.Ratio)
                        ? "inf"
                        : item.Ratio.ToString("0.000");

                output.AppendLine(
                    "x" +
                    (item.OriginalIndex + 1) +
                    "  profit=" +
                    item.Profit.ToString("0.000") +
                    "  weight=" +
                    item.Weight.ToString("0.000") +
                    "  ratio=" +
                    ratio);
            }

            output.AppendLine();
        }

        private void PrintNode(
            int nodeNumber,
            int level,
            double weight,
            double profit,
            double upperBound,
            int[] decisions)
        {
            output.AppendLine("----------------------------------------");
            output.AppendLine("NODE " + nodeNumber);
            output.AppendLine("----------------------------------------");

            output.AppendLine(
                "level = " + level);

            output.AppendLine(
                "weight = " +
                weight.ToString("0.000"));

            output.AppendLine(
                "profit = " +
                profit.ToString("0.000"));

            string boundText =
                double.IsNegativeInfinity(upperBound)
                    ? "-inf"
                    : upperBound.ToString("0.000");

            output.AppendLine(
                "upper bound = " + boundText);

            StringBuilder decisionText =
                new StringBuilder();

            for (int i = 0; i < decisions.Length; i++)
            {
                if (i > 0)
                    decisionText.Append(", ");

                decisionText.Append("x");
                decisionText.Append(i + 1);
                decisionText.Append("=");

                if (decisions[i] == -1)
                    decisionText.Append("?");
                else
                    decisionText.Append(decisions[i]);
            }

            output.AppendLine(
                "decisions: " +
                decisionText);

        }

        private void PrintBestCandidate()
        {
            output.AppendLine();
            output.AppendLine(
                "========================================");

            output.AppendLine(
                "BEST CANDIDATE");

            output.AppendLine(
                "========================================");

            for (int i = 0; i < bestDecision.Length; i++)
            {
                output.AppendLine(
                    "x" +
                    (i + 1) +
                    " = " +
                    bestDecision[i]);
            }

            output.AppendLine(
                "total weight = " +
                bestWeight.ToString("0.000"));

            output.AppendLine(
                "optimal z = " +
                bestProfit.ToString("0.000"));

            output.AppendLine(
                "========================================");
        }

        private string BuildExpression(
            IList<double> coefficients)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < coefficients.Count; i++)
            {
                double value = coefficients[i];

                if (i > 0)
                {
                    sb.Append(
                        value >= 0 ? " + " : " - ");
                }
                else if (value < 0)
                {
                    sb.Append("-");
                }

                sb.Append(
                    Math.Abs(value).ToString("0.000"));

                sb.Append("x");
                sb.Append(i + 1);
            }

            return sb.ToString();
        }
    }
}