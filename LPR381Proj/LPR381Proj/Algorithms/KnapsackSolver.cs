using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381Proj.Algorithms
{
    internal class KnapsackSolver
    {
        private const double EPS = 0.0000001;

        private class Item
        {
            public int OriginalIndex;
            public double Profit;
            public double Weight;

            public double Ratio
            {
                get
                {
                    if (Math.Abs(Weight) < EPS)
                        return double.PositiveInfinity;

                    return Profit / Weight;
                }
            }
        }

        private class Node
        {
            public int Level;
            public double Weight;
            public double Profit;
            public double Bound;
            public int[] Decisions;
        }

        private List<Item> items;
        private double capacity;

        private double bestProfit;
        private double bestWeight;
        private int[] bestSolution;

        private int nodeNumber;
        private StringBuilder output;

        public int[] LastSolution { get; private set; }
        public double LastObjectiveValue { get; private set; }
        public double LastWeight { get; private set; }

        public string Solve(
            double[] profits,
            double[] weights,
            double capacityValue)
        {
            ValidateInput(profits, weights, capacityValue);

            capacity = capacityValue;
            bestProfit = 0;
            bestWeight = 0;
            nodeNumber = 0;

            output = new StringBuilder();

            bestSolution = new int[profits.Length];

            items = new List<Item>();

            for (int i = 0; i < profits.Length; i++)
            {
                items.Add(new Item
                {
                    OriginalIndex = i,
                    Profit = profits[i],
                    Weight = weights[i]
                });
            }

            // Branch & Bound Knapsack normally uses
            // descending profit/weight ratios for its bound.
            items = items
                .OrderByDescending(x => x.Ratio)
                .ThenBy(x => x.OriginalIndex)
                .ToList();

            output.AppendLine("========================================");
            output.AppendLine("BRANCH AND BOUND KNAPSACK");
            output.AppendLine("========================================");
            output.AppendLine();

            DisplayModel(profits, weights);

            output.AppendLine();
            output.AppendLine("ITEM ORDER");
            output.AppendLine("----------------------------------------");

            foreach (Item item in items)
            {
                output.AppendLine(
                    "x" + (item.OriginalIndex + 1) +
                    "  profit=" + F(item.Profit) +
                    "  weight=" + F(item.Weight) +
                    "  ratio=" + F(item.Ratio));
            }

            int[] decisions =
                Enumerable.Repeat(-1, items.Count).ToArray();

            Node root = new Node
            {
                Level = 0,
                Weight = 0,
                Profit = 0,
                Decisions = decisions
            };

            root.Bound = CalculateBound(root);

            output.AppendLine();
            output.AppendLine("SEARCH TREE");
            output.AppendLine("========================================");

            Branch(root);

            LastSolution = (int[])bestSolution.Clone();
            LastObjectiveValue = bestProfit;
            LastWeight = bestWeight;

            output.AppendLine();
            output.AppendLine("========================================");
            output.AppendLine("BEST CANDIDATE");
            output.AppendLine("========================================");

            for (int i = 0; i < bestSolution.Length; i++)
            {
                output.AppendLine(
                    "x" + (i + 1) + " = " + bestSolution[i]);
            }

            output.AppendLine("total weight = " + F(bestWeight));
            output.AppendLine("optimal z = " + F(bestProfit));

            output.AppendLine("========================================");

            string result = output.ToString();

            Console.WriteLine(result);

            return result;
        }

        private void Branch(Node node)
        {
            nodeNumber++;

            int currentNode = nodeNumber;

            DisplayNode(currentNode, node);

            // -------------------------------------------------
            // FATHOM: infeasible
            // -------------------------------------------------
            if (node.Weight > capacity + EPS)
            {
                output.AppendLine("status: fathomed - infeasible");
                output.AppendLine();
                return;
            }

            // A partial node is itself a valid binary solution
            // when all currently undecided variables are 0.
            if (node.Profit > bestProfit + EPS)
            {
                bestProfit = node.Profit;
                bestWeight = node.Weight;

                bestSolution =
                    ConvertToOriginalOrder(node.Decisions, true);

                output.AppendLine("status: new candidate");
                output.AppendLine(
                    "candidate z = " + F(bestProfit));
            }

            // -------------------------------------------------
            // FATHOM: all variables have been fixed
            // -------------------------------------------------
            if (node.Level >= items.Count)
            {
                output.AppendLine(
                    "status: fathomed - complete integer solution");

                output.AppendLine();
                return;
            }

            node.Bound = CalculateBound(node);

            // -------------------------------------------------
            // FATHOM: upper bound cannot improve incumbent
            // -------------------------------------------------
            if (node.Bound <= bestProfit + EPS)
            {
                output.AppendLine(
                    "status: fathomed - bound");

                output.AppendLine(
                    "upper bound cannot improve best candidate");

                output.AppendLine();
                return;
            }

            Item item = items[node.Level];

            output.AppendLine(
                "branching variable: x" +
                (item.OriginalIndex + 1));

            output.AppendLine();

            // =================================================
            // LEFT SUB-PROBLEM: xi = 1
            // =================================================
            int[] includeDecisions =
                (int[])node.Decisions.Clone();

            includeDecisions[node.Level] = 1;

            Node includeNode = new Node
            {
                Level = node.Level + 1,

                Weight =
                    node.Weight + item.Weight,

                Profit =
                    node.Profit + item.Profit,

                Decisions = includeDecisions
            };

            includeNode.Bound =
                CalculateBound(includeNode);

            output.AppendLine(
                "sub-problem: x" +
                (item.OriginalIndex + 1) +
                " = 1");

            Branch(includeNode);

            // =================================================
            // BACKTRACK
            // =================================================
            output.AppendLine(
                "backtrack from x" +
                (item.OriginalIndex + 1) +
                " = 1");

            // =================================================
            // RIGHT SUB-PROBLEM: xi = 0
            // =================================================
            int[] excludeDecisions =
                (int[])node.Decisions.Clone();

            excludeDecisions[node.Level] = 0;

            Node excludeNode = new Node
            {
                Level = node.Level + 1,
                Weight = node.Weight,
                Profit = node.Profit,
                Decisions = excludeDecisions
            };

            excludeNode.Bound =
                CalculateBound(excludeNode);

            output.AppendLine(
                "sub-problem: x" +
                (item.OriginalIndex + 1) +
                " = 0");

            Branch(excludeNode);

            output.AppendLine(
                "backtrack from x" +
                (item.OriginalIndex + 1) +
                " = 0");
        }

        private double CalculateBound(Node node)
        {
            if (node.Weight > capacity + EPS)
                return double.NegativeInfinity;

            double bound = node.Profit;
            double weight = node.Weight;

            int i = node.Level;

            // Add complete remaining items.
            while (i < items.Count &&
                   weight + items[i].Weight <= capacity + EPS)
            {
                weight += items[i].Weight;
                bound += items[i].Profit;

                i++;
            }

            // Add a fractional item ONLY when calculating
            // the upper bound.
            if (i < items.Count &&
                weight < capacity - EPS)
            {
                double remaining =
                    capacity - weight;

                bound +=
                    remaining * items[i].Ratio;
            }

            return bound;
        }

        private void DisplayNode(int number, Node node)
        {
            output.AppendLine("----------------------------------------");
            output.AppendLine("NODE " + number);
            output.AppendLine("----------------------------------------");

            output.AppendLine(
                "level = " + node.Level);

            output.AppendLine(
                "weight = " + F(node.Weight));

            output.AppendLine(
                "profit = " + F(node.Profit));

            output.AppendLine(
                "upper bound = " + F(node.Bound));

            output.AppendLine(
                "decisions: " +
                DecisionText(node.Decisions));
        }

        private string DecisionText(int[] sortedDecisions)
        {
            int[] original =
                ConvertToOriginalOrder(
                    sortedDecisions,
                    false);

            StringBuilder result =
                new StringBuilder();

            for (int i = 0; i < original.Length; i++)
            {
                result.Append("x");
                result.Append(i + 1);
                result.Append("=");

                if (original[i] == -1)
                    result.Append("?");
                else
                    result.Append(original[i]);

                if (i < original.Length - 1)
                    result.Append(", ");
            }

            return result.ToString();
        }

        private int[] ConvertToOriginalOrder(
            int[] sortedDecisions,
            bool unknownAsZero)
        {
            int defaultValue =
                unknownAsZero ? 0 : -1;

            int[] original =
                Enumerable
                .Repeat(defaultValue, items.Count)
                .ToArray();

            for (int i = 0; i < items.Count; i++)
            {
                int originalIndex =
                    items[i].OriginalIndex;

                int value = sortedDecisions[i];

                if (value == -1 && unknownAsZero)
                    value = 0;

                original[originalIndex] = value;
            }

            return original;
        }

        private void DisplayModel(
            double[] profits,
            double[] weights)
        {
            output.Append("max z = ");

            for (int i = 0; i < profits.Length; i++)
            {
                if (i > 0)
                    output.Append(" + ");

                output.Append(
                    F(profits[i]) +
                    "x" + (i + 1));
            }

            output.AppendLine();

            output.Append("s.t. ");

            for (int i = 0; i < weights.Length; i++)
            {
                if (i > 0)
                    output.Append(" + ");

                output.Append(
                    F(weights[i]) +
                    "x" + (i + 1));
            }

            output.AppendLine(
                " <= " + F(capacity));

            output.AppendLine(
                "x1 ... x" +
                profits.Length +
                " binary");
        }

        private void ValidateInput(
            double[] profits,
            double[] weights,
            double capacityValue)
        {
            if (profits == null || weights == null)
                throw new ArgumentException(
                    "profits and weights cannot be null.");

            if (profits.Length == 0)
                throw new ArgumentException(
                    "at least one item is required.");

            if (profits.Length != weights.Length)
                throw new ArgumentException(
                    "profits and weights must have equal lengths.");

            if (capacityValue < 0)
                throw new ArgumentException(
                    "capacity cannot be negative.");

            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] < 0)
                    throw new ArgumentException(
                        "weights cannot be negative.");
            }
        }

        private string F(double value)
        {
            if (double.IsPositiveInfinity(value))
                return "inf";

            if (double.IsNegativeInfinity(value))
                return "-inf";

            return Math.Round(value, 3)
                .ToString("0.000");
        }
    }
}