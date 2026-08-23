using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LPR381Proj.Models;

namespace LPR381Proj.Input
{
    public class InputParser
    {
        public LinearProgram ParseFile(string filePath)
        {
            string[] rawLines = File.ReadAllLines(filePath)
                                   .Where(l => !string.IsNullOrWhiteSpace(l))
                                   .Select(l => l.Trim())
                                   .ToArray();

            LinearProgram lp = new LinearProgram();

            // 1. Parse Objective Line
            string objLine = CleanSpacing(rawLines[0]);
            string[] objTokens = objLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            lp.Objective = objTokens[0].Equals("max", StringComparison.OrdinalIgnoreCase)
                ? ObjectiveType.Maximize
                : ObjectiveType.Minimize;

            for (int i = 1; i < objTokens.Length; i++)
            {
                lp.ObjectiveCoefficients.Add(ParseNumberOrFraction(objTokens[i]));
            }

            int targetVarCount = lp.ObjectiveCoefficients.Count;

            // 2. Parse Variable Types (Last Line)
            string typeLine = CleanSpacing(rawLines[rawLines.Length - 1]);
            string[] typeTokens = typeLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in typeTokens)
            {
                string token = t.ToLower();
                if (token == "bin") lp.VariableTypes.Add(VariableType.Binary);
                else if (token == "int") lp.VariableTypes.Add(VariableType.Integer);
                else if (token == "urs") lp.VariableTypes.Add(VariableType.Unrestricted);
                else if (token == "-") lp.VariableTypes.Add(VariableType.NonPositive);
                else lp.VariableTypes.Add(VariableType.Continuous);
            }

            // 3. Parse Constraints (Middle Lines)
            for (int i = 1; i < rawLines.Length - 1; i++)
            {
                string line = CleanSpacing(rawLines[i]);
                RelationType rel = RelationType.LessThanOrEqual;
                string relStr = "<=";

                if (line.Contains("<=")) { rel = RelationType.LessThanOrEqual; relStr = "<="; }
                else if (line.Contains(">=")) { rel = RelationType.GreaterThanOrEqual; relStr = ">="; }
                else if (line.Contains("=")) { rel = RelationType.Equal; relStr = "="; }

                string[] parts = line.Split(new[] { relStr }, StringSplitOptions.RemoveEmptyEntries);

                string[] coeffTokens = parts[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<double> coeffs = new List<double>();

                foreach (var token in coeffTokens)
                {
                    coeffs.Add(ParseNumberOrFraction(token));
                }

                while (coeffs.Count < targetVarCount)
                {
                    coeffs.Add(0.0);
                }

                double rhs = ParseNumberOrFraction(parts[1]);
                lp.Constraints.Add(new Constraint(coeffs, rel, rhs));
            }

            return lp;
        }

        private string CleanSpacing(string input)
        {
            // Fixes spacing between sign and digits or fraction numerators (e.g., "+ 1/40" -> "+1/40")
            string cleaned = Regex.Replace(input, @"([+-])\s+(\d)", "$1$2");
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }

        private double ParseNumberOrFraction(string token)
        {
            token = token.Trim();
            if (token == "+") return 1.0;
            if (token == "-") return -1.0;

            if (token.Contains("/"))
            {
                string[] parts = token.Split('/');
                if (parts.Length == 2)
                {
                    double numerator = double.Parse(parts[0]);
                    double denominator = double.Parse(parts[1]);
                    return numerator / denominator;
                }
            }

            return double.Parse(token);
        }
    }
}