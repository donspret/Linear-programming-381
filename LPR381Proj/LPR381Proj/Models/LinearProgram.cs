using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381Proj.Models
{
    public enum ObjectiveType
    {
        Maximize,
        Minimize
    }

    public class LinearProgram
    {
        public ObjectiveType Objective { get; set; }
        public List<double> ObjectiveCoefficients { get; set; } = new List<double>();
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        public List<VariableType> VariableTypes { get; set; } = new List<VariableType>();
        public int VariableCount => ObjectiveCoefficients.Count;
    }
}
