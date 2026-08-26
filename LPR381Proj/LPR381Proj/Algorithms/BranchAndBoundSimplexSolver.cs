using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Models;

namespace LPR381Proj.Algorithms
{
    internal class BranchAndBoundSimplexSolver
    {
        private const double IntegerTolerance = 1e-6;
        private const int MaxNodes = 5000; 

        private readonly BranchAndBoundNodeSolver _nodeSolver = new BranchAndBoundNodeSolver();

        public BranchAndBoundResult Solve(IntegerProgram ip)
        {
            BranchAndBoundResult result = new BranchAndBoundResult();
            bool maximize = ip.Objective == ObjectiveType.Maximize;

            
            LinearProgram rootLp = CloneLp(ip);

            
            Stack<PendingNode> stack = new Stack<PendingNode>();
            stack.Push(new PendingNode { Lp = rootLp, ParentId = 0, BranchDescription = "Root (LP Relaxation)" });

            int nodeCounter = 0;
            Solution bestSolution = null;
            int bestNodeId = -1;

            while (stack.Count > 0)
            {
                if (nodeCounter >= MaxNodes)
                {
                    result.NodeLimitReached = true;
                    break;
                }

                PendingNode pending = stack.Pop();
                nodeCounter++;

                BranchAndBoundNodeResult node = _nodeSolver.Solve(pending.Lp, nodeCounter, pending.ParentId, pending.BranchDescription);
                result.Nodes.Add(node);

                if (node.Solution.Status == SolutionStatus.Infeasible)
                {
                    node.Fathomed = true;
                    node.FathomReason = "infeasible";
                    continue;
                }

                if (node.Solution.Status == SolutionStatus.Unbounded)
                {
                    node.Fathomed = true;
                    node.FathomReason = "unbounded";
                    continue;
                }

                int branchVarIndex = FindBranchVariable(pending.Lp, node.Solution);

                if (branchVarIndex == -1)
                {
                    
                    node.Fathomed = true;
                    node.FathomReason = "integer candidate ";

                    if (bestSolution == null || CanImprove(node.Solution.OptimalValue, bestSolution.OptimalValue, maximize))
                    {
                        bestSolution = node.Solution;
                        bestNodeId = node.NodeId;
                    }

                    continue;
                }

                string varName = $"x{branchVarIndex + 1}";
                double value = node.Solution.VariableValues.TryGetValue(varName, out double v) ? v : 0.0;
                double floorBound = Math.Floor(value + IntegerTolerance);
                double ceilBound = Math.Ceiling(value - IntegerTolerance);

                LinearProgram geLp = CloneLp(pending.Lp);
                AddBoundConstraint(geLp, branchVarIndex, RelationType.GreaterThanOrEqual, ceilBound);
                stack.Push(new PendingNode
                {
                    Lp = geLp,
                    ParentId = node.NodeId,
                    BranchDescription = $"{varName} >= {ceilBound:0.###} (from node {node.NodeId})"
                });

                LinearProgram leLp = CloneLp(pending.Lp);
                AddBoundConstraint(leLp, branchVarIndex, RelationType.LessThanOrEqual, floorBound);
                stack.Push(new PendingNode
                {
                    Lp = leLp,
                    ParentId = node.NodeId,
                    BranchDescription = $"{varName} <= {floorBound:0.###} (from node {node.NodeId})"
                });
            }

            result.BestSolution = bestSolution;
            result.BestNodeId = bestNodeId;
            return result;
        }

        private bool CanImprove(double candidate, double incumbent, bool maximize)
        {
            return maximize
                ? candidate > incumbent + IntegerTolerance
                : candidate < incumbent - IntegerTolerance;
        }

        
        private int FindBranchVariable(LinearProgram nodeLp, Solution solution)
        {
            for (int i = 0; i < nodeLp.VariableTypes.Count; i++)
            {
                if (nodeLp.VariableTypes[i] != VariableType.Integer && nodeLp.VariableTypes[i] != VariableType.Binary)
                    continue;

                string varName = $"x{i + 1}";
                if (!solution.VariableValues.TryGetValue(varName, out double value))
                    continue;

                double rounded = Math.Round(value);
                if (Math.Abs(value - rounded) > IntegerTolerance)
                    return i;
            }

            return -1;
        }

        private void AddBoundConstraint(LinearProgram lp, int variableIndex, RelationType relation, double rhs)
        {
            List<double> coeffs = new List<double>(new double[lp.VariableCount]);
            coeffs[variableIndex] = 1.0;
            lp.Constraints.Add(new Constraint(coeffs, relation, rhs));
        }

        private LinearProgram CloneLp(LinearProgram source)
        {
            LinearProgram clone = new LinearProgram
            {
                Objective = source.Objective,
                ObjectiveCoefficients = new List<double>(source.ObjectiveCoefficients),
                VariableTypes = new List<VariableType>(source.VariableTypes)
            };

            foreach (Constraint c in source.Constraints)
                clone.Constraints.Add(new Constraint(new List<double>(c.Coefficients), c.Relation, c.RHS));

            return clone;
        }
    }

   
    internal class PendingNode
    {
        public LinearProgram Lp { get; set; }
        public int ParentId { get; set; }
        public string BranchDescription { get; set; }
    }

   
    internal class BranchAndBoundResult
    {
        public List<BranchAndBoundNodeResult> Nodes { get; set; } = new List<BranchAndBoundNodeResult>();
        public Solution BestSolution { get; set; }
        public int BestNodeId { get; set; } = -1;
        public bool NodeLimitReached { get; set; }
    }
}
