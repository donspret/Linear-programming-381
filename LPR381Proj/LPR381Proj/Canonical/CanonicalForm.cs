using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR381Proj.Models;
namespace LPR381Proj.Canonical
{
    public class CanonicalForm
    {
        public double[,] Tableau { get; set; }
        public List<string> VariableNames { get; set; } = new List<string>();
        public List<string> BasicVariables { get; set; } = new List<string>();
        public int NumConstraints { get; set; }
        public int NumVariables { get; set; }

        public static CanonicalForm ConvertToCanonical(LinearProgram lp)
        {
            CanonicalForm cf = new CanonicalForm();
            int origVars = lp.VariableCount;
            int numConstraints = lp.Constraints.Count;

            cf.NumConstraints = numConstraints;

            for (int i = 1; i <= origVars; i++)
                cf.VariableNames.Add($"x{i}");

            int slackCount = 0;
            for (int i = 0; i < numConstraints; i++)
            {
                slackCount++;
                string sName = $"s{slackCount}";
                cf.VariableNames.Add(sName);
                cf.BasicVariables.Add(sName);
            }

            cf.NumVariables = cf.VariableNames.Count;
            // Rows: Objective row + constraints. Cols: Variables + RHS
            cf.Tableau = new double[numConstraints + 1, cf.NumVariables + 1];

            // Fill Objective Row (Max standard form: Z - c_1*x_1 - ... = 0)
            double multiplier = lp.Objective == ObjectiveType.Maximize ? -1.0 : 1.0;
            for (int j = 0; j < origVars; j++)
            {
                cf.Tableau[0, j] = multiplier * lp.ObjectiveCoefficients[j];
            }

            // Fill Constraint Rows
            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = lp.Constraints[i];
                for (int j = 0; j < origVars; j++)
                {
                    cf.Tableau[i + 1, j] = constraint.Coefficients[j];
                }

                // Add Slack identity matrix component
                cf.Tableau[i + 1, origVars + i] = 1.0;

                // Set RHS
                cf.Tableau[i + 1, cf.NumVariables] = constraint.RHS;
            }

            return cf;
        }
    }
}
