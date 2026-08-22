using UnityEditor;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace ThunderKit.Core.Paths
{
    // Pipeline logs render assetlink:// URIs as clickable links to the offending
    // asset, so every path resolution failure is reported through this shape.
    internal static class PathDiagnostics
    {
        public static string Link(PathReference output, PathComponent component, string suffix = null)
        {
            return Link(output, $"{Describe(output)}.{Describe(component)}{suffix}");
        }

        public static string Link(PathReference output, string label)
        {
            var assetPath = UnityWebRequest.EscapeURL(AssetDatabase.GetAssetPath(output));
            return $"[{label}](assetlink://{assetPath})";
        }

        static string Describe(Object target)
        {
            return target ? target.name : "<unassigned>";
        }
    }
}
