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
        // Big-M penalty used to drive artificial variables out of the basis.
        // Large relative to typical textbook coefficients, but not so large it blows up double precision.
        public const double M = 1_000_000.0;

        public double[,] Tableau { get; set; }
        public List<string> VariableNames { get; set; } = new List<string>();
        public List<string> BasicVariables { get; set; } = new List<string>();
        public int NumConstraints { get; set; }
        public int NumVariables { get; set; }

        // The problem's original objective sense, needed to convert the internal
        // max(-z) result back to the real z when the user asked to Minimize.
        public ObjectiveType Objective { get; set; }

        // Column indices of every artificial variable, used for the Big-M elimination
        // step and for the post-solve infeasibility check.
        public List<int> ArtificialColumns { get; set; } = new List<int>();

        // Maps each ORIGINAL decision variable (x1, x2, ...) back to the column(s)
        // that represent it in the tableau, so the solver can reconstruct the real
        // value after solving (needed for urs-split and NonPositive-substituted vars).
        public List<VariableMapping> VariableMap { get; set; } = new List<VariableMapping>();

        public class VariableMapping
        {
            public string OriginalName;
            public VariableType Kind;
            public int PosIndex;      // column holding x (Continuous/NonPositive) or x+ (Unrestricted)
            public int NegIndex = -1; // column holding x- (Unrestricted only), else -1
        }

        private class NormalizedConstraint
        {
            public List<double> Coeffs;
            public RelationType Rel;
            public double Rhs;
        }

        public static CanonicalForm ConvertToCanonical(LinearProgram lp)
        {
            CanonicalForm cf = new CanonicalForm();
            cf.Objective = lp.Objective;

            int origVars = lp.VariableCount;

            // --- Step 0: Append upper bounds (x_j <= 1) for Binary variables ---
            List<Constraint> inputConstraints = new List<Constraint>(lp.Constraints);
            for (int j = 0; j < origVars; j++)
            {
                var vtype = (j < lp.VariableTypes.Count) ? lp.VariableTypes[j] : VariableType.Continuous;
                if (vtype == VariableType.Binary)
                {
                    List<double> binCoeffs = new List<double>();
                    for (int k = 0; k < origVars; k++)
                    {
                        binCoeffs.Add(k == j ? 1.0 : 0.0);
                    }
                    inputConstraints.Add(new Constraint(binCoeffs, RelationType.LessThanOrEqual, 1.0));
                }
            }

            int numConstraints = inputConstraints.Count;
            cf.NumConstraints = numConstraints;

            // --- Step 1: normalize constraints so every RHS >= 0 ---
            // (Flipping the row's sign when RHS < 0 also flips <= into >= and vice versa;
            // Equal stays Equal. This is required before we can safely add a surplus/slack.)
            var norm = new List<NormalizedConstraint>();
            foreach (var c in inputConstraints)
            {
                var coeffs = new List<double>(c.Coefficients);

                // Ensure the list is properly padded to match original variable count
                while (coeffs.Count < origVars)
                {
                    coeffs.Add(0.0);
                }

                var rel = c.Relation;
                var rhs = c.RHS;

                if (rhs < 0)
                {
                    coeffs = coeffs.Select(v => -v).ToList();
                    rhs = -rhs;
                    if (rel == RelationType.LessThanOrEqual) rel = RelationType.GreaterThanOrEqual;
                    else if (rel == RelationType.GreaterThanOrEqual) rel = RelationType.LessThanOrEqual;
                }

                norm.Add(new NormalizedConstraint { Coeffs = coeffs, Rel = rel, Rhs = rhs });
            }

            // --- Step 2: build decision-variable columns, honoring sign restrictions ---
            // Continuous/Binary/Integer -> single column, x >= 0
            // NonPositive ("-")         -> single column, substituted x = -y, y >= 0
            // Unrestricted ("urs")      -> two columns, x = x+ - x-, both >= 0
            for (int i = 0; i < origVars; i++)
            {
                string baseName = $"x{i + 1}";
                var vtype = (i < lp.VariableTypes.Count) ? lp.VariableTypes[i] : VariableType.Continuous;

                var map = new VariableMapping { OriginalName = baseName, Kind = vtype };

                if (vtype == VariableType.Unrestricted)
                {
                    map.PosIndex = cf.VariableNames.Count;
                    cf.VariableNames.Add($"{baseName}+");
                    map.NegIndex = cf.VariableNames.Count;
                    cf.VariableNames.Add($"{baseName}-");
                }
                else
                {
                    map.PosIndex = cf.VariableNames.Count;
                    cf.VariableNames.Add(baseName);
                }

                cf.VariableMap.Add(map);
            }

            // --- Step 3: decide slack / surplus / artificial columns per constraint ---
            var slackCol = new int[numConstraints];
            var surplusCol = new int[numConstraints];
            var artificialCol = new int[numConstraints];
            int slackCount = 0, surplusCount = 0, artificialCount = 0;

            for (int i = 0; i < numConstraints; i++)
            {
                slackCol[i] = surplusCol[i] = artificialCol[i] = -1;
                var rel = norm[i].Rel;

                if (rel == RelationType.LessThanOrEqual)
                {
                    slackCol[i] = cf.VariableNames.Count;
                    cf.VariableNames.Add($"s{++slackCount}");
                }
                else if (rel == RelationType.GreaterThanOrEqual)
                {
                    surplusCol[i] = cf.VariableNames.Count;
                    cf.VariableNames.Add($"e{++surplusCount}");

                    artificialCol[i] = cf.VariableNames.Count;
                    cf.VariableNames.Add($"a{++artificialCount}");
                    cf.ArtificialColumns.Add(artificialCol[i]);
                }
                else // Equal
                {
                    artificialCol[i] = cf.VariableNames.Count;
                    cf.VariableNames.Add($"a{++artificialCount}");
                    cf.ArtificialColumns.Add(artificialCol[i]);
                }
            }

            cf.NumVariables = cf.VariableNames.Count;
            cf.Tableau = new double[numConstraints + 1, cf.NumVariables + 1];

            // --- Step 4: fill the objective row ---
            // Internal convention (matches the existing pivot rule "most negative wins"):
            //   row0[j] = -(effective coefficient of column j in an equivalent MAX problem)
            // effective coefficient = c_j for Maximize, -c_j for Minimize (standard max(-z) trick),
            // 0 for slack/surplus, and -M for every artificial (heavy penalty).
            double sign = lp.Objective == ObjectiveType.Maximize ? 1.0 : -1.0;

            for (int i = 0; i < origVars; i++)
            {
                double c = i < lp.ObjectiveCoefficients.Count ? lp.ObjectiveCoefficients[i] : 0.0;
                var map = cf.VariableMap[i];

                if (map.Kind == VariableType.NonPositive)
                {
                    // x = -y  =>  c*x = -c*y
                    cf.Tableau[0, map.PosIndex] = -(sign * (-c));
                }
                else
                {
                    cf.Tableau[0, map.PosIndex] = -(sign * c);
                    if (map.NegIndex != -1)
                        cf.Tableau[0, map.NegIndex] = -(sign * (-c));
                }
            }
            foreach (var aCol in cf.ArtificialColumns)
            {
                cf.Tableau[0, aCol] = -(-M); // = +M, eliminated below since it starts basic
            }

            // --- Step 5: fill constraint rows ---
            for (int i = 0; i < numConstraints; i++)
            {
                var coeffs = norm[i].Coeffs;
                var rel = norm[i].Rel;
                var rhs = norm[i].Rhs;

                for (int j = 0; j < origVars; j++)
                {
                    var map = cf.VariableMap[j];
                    if (map.Kind == VariableType.NonPositive)
                    {
                        cf.Tableau[i + 1, map.PosIndex] = -coeffs[j];
                    }
                    else
                    {
                        cf.Tableau[i + 1, map.PosIndex] = coeffs[j];
                        if (map.NegIndex != -1)
                            cf.Tableau[i + 1, map.NegIndex] = -coeffs[j];
                    }
                }

                if (slackCol[i] != -1) cf.Tableau[i + 1, slackCol[i]] = 1.0;
                if (surplusCol[i] != -1) cf.Tableau[i + 1, surplusCol[i]] = -1.0;
                if (artificialCol[i] != -1) cf.Tableau[i + 1, artificialCol[i]] = 1.0;

                cf.Tableau[i + 1, cf.NumVariables] = rhs;

                // Initial basic variable for this row: slack if we have one, else the artificial.
                cf.BasicVariables.Add(slackCol[i] != -1
                    ? cf.VariableNames[slackCol[i]]
                    : cf.VariableNames[artificialCol[i]]);
            }

            // --- Step 6: Big-M elimination ---
            // Every artificial starts in the basis, so its objective-row entry (M) must be
            // zeroed out by subtracting M * (its constraint row) from row 0, same as
            // pivoting would do -- this keeps the tableau consistent with "basic columns
            // have a 0 in row 0" before the first real iteration.
            for (int i = 0; i < numConstraints; i++)
            {
                if (artificialCol[i] != -1 && cf.BasicVariables[i] == cf.VariableNames[artificialCol[i]])
                {
                    double factor = cf.Tableau[0, artificialCol[i]];
                    for (int j = 0; j <= cf.NumVariables; j++)
                    {
                        cf.Tableau[0, j] -= factor * cf.Tableau[i + 1, j];
                    }
                }
            }

            return cf;
        }
    }
}