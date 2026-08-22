using System;
using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class OutputReference : PathComponent
    {
        public PathReference reference;
        protected override string GetPathInternal(PathReference output, Pipeline pipeline)
        {
            try
            {
                return reference.GetPath(pipeline);
            }
            catch (NullReferenceException nre)
            {
                var pathReferenceLink = PathDiagnostics.Link(output, this, ".reference");
                throw new InvalidOperationException($"Error {pathReferenceLink} is unassigned or null", nre);
            }
            catch (Exception e)
            {
                var pathReferenceLink = PathDiagnostics.Link(output, this, $".reference({reference.name})");
                throw new InvalidOperationException($"Error Invoking PathReference: {pathReferenceLink}", e);
            }
        }
    }
}
