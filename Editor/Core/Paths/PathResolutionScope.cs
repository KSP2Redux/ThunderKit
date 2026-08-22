using System;
using System.Collections.Generic;
using System.Linq;

namespace ThunderKit.Core.Paths
{
    // PathReference resolution re-enters itself through OutputReference and through
    // Resolver's <Name> tokens. An authoring cycle would otherwise recurse until
    // StackOverflowException, which cannot be caught and terminates the Editor.
    internal sealed class PathResolutionScope : IDisposable
    {
        [ThreadStatic]
        static List<PathReference> resolving;

        public static PathResolutionScope Enter(PathReference reference)
        {
            if (resolving == null)
                resolving = new List<PathReference>();

            var cycleStart = resolving.IndexOf(reference);
            if (cycleStart >= 0)
                throw new InvalidOperationException(CycleMessage(cycleStart, reference));

            resolving.Add(reference);
            return new PathResolutionScope();
        }

        public void Dispose()
        {
            resolving.RemoveAt(resolving.Count - 1);
        }

        static string CycleMessage(int cycleStart, PathReference repeated)
        {
            var chain = resolving.Skip(cycleStart)
                                 .Concat(new[] { repeated })
                                 .Select(reference => PathDiagnostics.Link(reference, reference ? reference.name : "<unassigned>"));

            return $"PathReference cycle detected: {string.Join(" -> ", chain.ToArray())}";
        }
    }
}
