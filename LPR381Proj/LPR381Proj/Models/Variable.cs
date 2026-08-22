using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381Proj.Models
{
    public enum VariableType
    {
        Continuous,
        Binary,
        Integer,
        Slack,
        Surplus,
        Artificial,
        Unrestricted,   // urs: free in sign, split into x+ - x- in canonical form
        NonPositive     // "-": x <= 0, substituted as x = -y, y >= 0
    }

    public class Variable
    {
        public string Name { get; set; }
        public double Coefficient { get; set; }
        public VariableType Type { get; set; }

        public Variable(string name, double coefficient, VariableType type = VariableType.Continuous)
        {
            Name = name;
            Coefficient = coefficient;
            Type = type;
        }
    }
}
