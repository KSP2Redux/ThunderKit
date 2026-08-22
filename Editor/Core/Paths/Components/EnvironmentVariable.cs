using System;
using ThunderKit.Core.Pipelines;
using UnityEngine;

namespace ThunderKit.Core.Paths.Components
{
    public class EnvironmentVariable : PathComponent
    {
        [Tooltip("Name only, without % or $. Names differ per platform; prefer SpecialFolder for a portable location")]
        public string VariableName;

        [Tooltip("Used when the variable is not set. Leave empty to require it")]
        public string Fallback;

        protected override string GetPathInternal(PathReference output, Pipeline pipeline)
        {
            if (string.IsNullOrEmpty(VariableName))
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, this)} has no variable name assigned.");

            var value = Environment.GetEnvironmentVariable(VariableName);
            if (!string.IsNullOrEmpty(value))
                return value;

            if (!string.IsNullOrEmpty(Fallback))
                return Fallback;

            throw new InvalidOperationException(
                $"{PathDiagnostics.Link(output, this)} requires environment variable \"{VariableName}\", " +
                "which is not set for the Editor process. The Editor reads the environment it was " +
                "launched with, so a newly added variable needs an Editor restart.");
        }
    }
}
