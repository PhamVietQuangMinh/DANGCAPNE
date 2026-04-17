using System.Globalization;
using DANGCAPNE.Models.Workflow;

namespace DANGCAPNE.Services
{
    public interface IWorkflowConditionEvaluator
    {
        bool ShouldIncludeStep(WorkflowStep step, IReadOnlyDictionary<string, string> formData);
    }

    public class WorkflowConditionEvaluator : IWorkflowConditionEvaluator
    {
        public bool ShouldIncludeStep(WorkflowStep step, IReadOnlyDictionary<string, string> formData)
        {
            if (step.Conditions == null || step.Conditions.Count == 0)
            {
                return true;
            }

            return step.Conditions.Any(condition => Evaluate(condition, formData));
        }

        private static bool Evaluate(WorkflowCondition condition, IReadOnlyDictionary<string, string> formData)
        {
            if (!TryGetFieldValue(formData, condition.FieldName, out var rawValue))
            {
                return false;
            }

            var conditionValue = condition.Value?.Trim() ?? string.Empty;
            var actualValue = rawValue?.Trim() ?? string.Empty;

            if (double.TryParse(actualValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var actualNumber)
                && double.TryParse(conditionValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var conditionNumber))
            {
                return EvaluateNumber(condition.Operator, actualNumber, conditionNumber);
            }

            return EvaluateString(condition.Operator, actualValue, conditionValue);
        }

        private static bool TryGetFieldValue(IReadOnlyDictionary<string, string> formData, string fieldName, out string? value)
        {
            foreach (var pair in formData)
            {
                if (string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool EvaluateNumber(string? op, double actual, double expected)
        {
            return op switch
            {
                "GreaterThan" => actual > expected,
                "GreaterThanOrEqual" => actual >= expected,
                "LessThan" => actual < expected,
                "LessThanOrEqual" => actual <= expected,
                "Equals" => Math.Abs(actual - expected) < 0.0001,
                _ => false
            };
        }

        private static bool EvaluateString(string? op, string actual, string expected)
        {
            return op switch
            {
                "Equals" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                "Contains" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
                "StartsWith" => actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
                "EndsWith" => actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
