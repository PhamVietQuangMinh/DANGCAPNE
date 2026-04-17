using DANGCAPNE.Models.Workflow;
using DANGCAPNE.Services;
using Xunit;

namespace DANGCAPNE.Tests;

public class WorkflowConditionEvaluatorTests
{
    [Fact]
    public void ShouldIncludeStep_WhenNoConditions()
    {
        var evaluator = new WorkflowConditionEvaluator();
        var step = new WorkflowStep { Conditions = new List<WorkflowCondition>() };

        var result = evaluator.ShouldIncludeStep(step, new Dictionary<string, string>());

        Assert.True(result);
    }

    [Fact]
    public void ShouldIncludeStep_WhenAtLeastOneConditionMatched()
    {
        var evaluator = new WorkflowConditionEvaluator();
        var step = new WorkflowStep
        {
            Conditions = new List<WorkflowCondition>
            {
                new() { FieldName = "amount", Operator = "GreaterThan", Value = "10000000" },
                new() { FieldName = "department", Operator = "Equals", Value = "IT" }
            }
        };

        var result = evaluator.ShouldIncludeStep(step, new Dictionary<string, string>
        {
            ["amount"] = "15000000",
            ["department"] = "HR"
        });

        Assert.True(result);
    }

    [Fact]
    public void ShouldNotIncludeStep_WhenNoConditionMatched()
    {
        var evaluator = new WorkflowConditionEvaluator();
        var step = new WorkflowStep
        {
            Conditions = new List<WorkflowCondition>
            {
                new() { FieldName = "priority", Operator = "Equals", Value = "Urgent" }
            }
        };

        var result = evaluator.ShouldIncludeStep(step, new Dictionary<string, string>
        {
            ["priority"] = "Normal"
        });

        Assert.False(result);
    }
}
