using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LPR381Proj.Input
{
    public static class InputValidator
    {
        public static bool ValidateFile(string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!File.Exists(filePath))
            {
                errorMessage = $"File not found at path: {filePath}";
                return false;
            }

            string[] lines = File.ReadAllLines(filePath)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToArray();

            if (lines.Length < 3)
            {
                errorMessage = "File must contain at least an Objective function, one Constraint, and Variable bounds.";
                return false;
            }

            // Validate Objective Line (Line 0)
            string objLine = lines[0].Trim();
            if (!objLine.StartsWith("max", StringComparison.OrdinalIgnoreCase) &&
                !objLine.StartsWith("min", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Objective line must start with 'max' or 'min'.";
                return false;
            }

            // Validate Variable Types (Last Line)
            string varTypeLine = lines[lines.Length - 1].Trim();
            string[] varTypes = varTypeLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int expectedVars = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            if (varTypes.Length != expectedVars)
            {
                errorMessage = $"Variable type count ({varTypes.Length}) does not match decision variable count ({expectedVars}).";
                return false;
            }

            return true;
        }
    }
}