using System;
using System.IO;
using System.Linq;
using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths
{
    // Every PathComponent result converges here, so this is the only place that can
    // enforce that a component cannot silently corrupt the assembled path.
    internal static class PathAssembler
    {
        static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        // Drive-relative segments ("C:") only carry that meaning where paths are DOS
        // shaped; ':' is an ordinary filename character elsewhere.
        static readonly bool DosPaths = Path.DirectorySeparatorChar == '\\';

        public static string Assemble(PathReference output, Pipeline pipeline, ComposableElement[] data)
        {
            if (data == null)
                return string.Empty;

            var components = data.OfType<PathComponent>().ToArray();
            var segments = new string[components.Length];
            for (int index = 0; index < components.Length; index++)
            {
                var component = components[index];
                segments[index] = Validated(output, component, component.GetPath(output, pipeline), index);
            }

            if (segments.Length == 0)
                return string.Empty;

            return Path.Combine(segments);
        }

        // Ordered so that Path.IsPathRooted and Path.Combine, which reject invalid
        // characters on the .NET Framework profile, are never reached with them.
        static string Validated(PathReference output, PathComponent component, string segment, int index)
        {
            if (segment == null)
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, component)} returned null. " +
                    "A PathComponent must return a path segment or an empty string.");

            if (segment.IndexOfAny(InvalidPathChars) >= 0)
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, component)} returned \"{segment}\", " +
                    "which contains characters that are not valid in a path.");

            if (DosPaths && IsDriveRelative(segment))
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, component)} returned the drive-relative path \"{segment}\", " +
                    "which resolves against the current directory of that drive rather than its root. " +
                    "Add a trailing separator to mean the drive root.");

            if (index > 0 && Path.IsPathRooted(segment))
                throw new InvalidOperationException(
                    $"{PathDiagnostics.Link(output, component)} returned the rooted path \"{segment}\" at position {index}. " +
                    "Combining a rooted segment discards every component before it; " +
                    "move it to the first position or make it relative.");

            return segment;
        }

        static bool IsDriveRelative(string segment)
        {
            if (segment.Length < 2 || segment[1] != ':' || !char.IsLetter(segment[0]))
                return false;

            if (segment.Length == 2)
                return true;

            return segment[2] != '\\' && segment[2] != '/';
        }
    }
}
