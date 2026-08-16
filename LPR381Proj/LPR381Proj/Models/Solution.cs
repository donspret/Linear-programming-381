using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381Proj.Models
{
    public enum SolutionStatus
    {
        Optimal,
        Unbounded,
        Infeasible,
        IterationLimitReached
    }

    public class Solution
    {
        public SolutionStatus Status { get; set; }
        public double OptimalValue { get; set; }
        public Dictionary<string, double> VariableValues { get; set; } = new Dictionary<string, double>();
        public List<string> TableauIterations { get; set; } = new List<string>();
    }
}
