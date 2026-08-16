using LPR381Proj.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR381Proj.Input
{
    public class InputParser
    {
        public LinearProgram ParseFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .Select(l => l.Trim())
                                .ToArray();

            LinearProgram lp = new LinearProgram();

            // 1. Parse Objective
            string[] objTokens = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            lp.Objective = objTokens[0].ToLower() == "max" ? ObjectiveType.Maximize : ObjectiveType.Minimize;

            for (int i = 1; i < objTokens.Length; i++)
            {
                lp.ObjectiveCoefficients.Add(double.Parse(objTokens[i]));
            }

            // 2. Parse Variable Types (Last Line)
            string[] typeTokens = lines[lines.Length - 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in typeTokens)
            {
                switch (t.ToLower())
                {
                    case "bin": lp.VariableTypes.Add(VariableType.Binary); break;
                    case "int": lp.VariableTypes.Add(VariableType.Integer); break;
                    default: lp.VariableTypes.Add(VariableType.Continuous); break;
                }
            }

            // 3. Parse Constraints (Middle Lines)
            for (int i = 1; i < lines.Length - 1; i++)
            {
                string line = lines[i];
                RelationType rel = RelationType.LessThanOrEqual;
                string relStr = "<=";

                if (line.Contains("<=")) { rel = RelationType.LessThanOrEqual; relStr = "<="; }
                else if (line.Contains(">=")) { rel = RelationType.GreaterThanOrEqual; relStr = ">="; }
                else if (line.Contains("=")) { rel = RelationType.Equal; relStr = "="; }

                string[] parts = line.Split(new[] { relStr }, StringSplitOptions.RemoveEmptyEntries);
                List<double> coeffs = parts[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(double.Parse)
                                              .ToList();
                double rhs = double.Parse(parts[1].Trim());

                lp.Constraints.Add(new Constraint(coeffs, rel, rhs));
            }

            return lp;
        }
    }
}