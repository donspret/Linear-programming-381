using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381Proj.Models
{
    public enum RelationType
    {
        LessThanOrEqual,    // <=
        GreaterThanOrEqual, // >=
        Equal               // =
    }

    public class Constraint
    {
        public List<double> Coefficients { get; set; } = new List<double>();
        public RelationType Relation { get; set; }
        public double RHS { get; set; }

        public Constraint(List<double> coefficients, RelationType relation, double rhs)
        {
            Coefficients = coefficients;
            Relation = relation;
            RHS = rhs;
        }
    }
}