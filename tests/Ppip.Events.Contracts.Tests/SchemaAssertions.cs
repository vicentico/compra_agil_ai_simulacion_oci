using System.Text.Json;
using Json.Schema;
using Xunit;

namespace Ppip.Events.Contracts.Tests;

internal static class SchemaAssertions
{
    public static void AssertValid(JsonSchema schema, JsonDocument instance)
    {
        var result = schema.Evaluate(instance.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (result.IsValid)
        {
            return;
        }

        var failures = result.Details
            .Where(d => !d.IsValid)
            .Select(d => d.Errors is null ? d.EvaluationPath.ToString() : $"{d.EvaluationPath}: {string.Join(',', d.Errors.Values)}");
        Assert.Fail(string.Join("; ", failures));
    }
}
