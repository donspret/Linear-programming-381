using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Canonical;
using LPR381Proj.Models;

namespace LPR381Proj.Algorithms
{
    internal class BranchAndBoundNodeSolver
    {
        private readonly PrimalSimplexSolver _simplex = new PrimalSimplexSolver();

        public BranchAndBoundNodeResult Solve(LinearProgram nodeLp, int nodeId, int parentId, string branchDescription)
        {
            CanonicalForm cf = CanonicalForm.ConvertToCanonical(nodeLp);
            Solution solution = _simplex.Solve(cf);

            return new BranchAndBoundNodeResult
            {
                NodeId = nodeId,
                ParentId = parentId,
                BranchDescription = branchDescription,
                CanonicalForm = cf,
                Solution = solution
            };
        }
    }

    internal class BranchAndBoundNodeResult
    {
        public int NodeId { get; set; }
        public int ParentId { get; set; }
        public string BranchDescription { get; set; } = "Root";
        public CanonicalForm CanonicalForm { get; set; }
        public Solution Solution { get; set; }
        public bool Fathomed { get; set; }
        public string FathomReason { get; set; } = string.Empty;
    }
}
