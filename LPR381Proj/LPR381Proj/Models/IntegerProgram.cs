using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381Proj.Models
{
    public class IntegerProgram : LinearProgram
    {
        public bool IsBinary { get; set; }

        public IntegerProgram(LinearProgram baseLp)
        {
            Objective = baseLp.Objective;
            ObjectiveCoefficients = baseLp.ObjectiveCoefficients;
            Constraints = baseLp.Constraints;
            VariableTypes = baseLp.VariableTypes;
            IsBinary = VariableTypes.TrueForAll(t => t == VariableType.Binary);
        }
    }
}