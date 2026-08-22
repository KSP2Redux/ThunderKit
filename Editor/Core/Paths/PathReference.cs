using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThunderKit.Common;
using ThunderKit.Core.Pipelines;
using ThunderKit.Core.Utilities;
using UnityEditor;
using UnityEngine.Networking;

namespace ThunderKit.Core.Paths
{
    public class PathReference : ComposableObject
    {
        [MenuItem(Constants.ThunderKitContextRoot + nameof(PathReference), false, priority = Constants.ThunderKitMenuPriority)]
        public static void Create() => ScriptableHelper.SelectNewAsset<PathReference>();

        const string pathReferenceCacheKey = "PathReferenceCache";
        const char opo = '<';
        const char opc = '>';
        private static readonly Regex referenceIdentifier = new Regex($"\\{opo}(.*?)\\{opc}", RegexOptions.Compiled);

        public static string ResolvePath(string input, Pipeline pipeline, UnityEngine.Object caller)
        {
            var result = input;

            Dictionary<string, PathReference> pathReferenceCache;
            if (!pipeline || pipeline.ExecutionInfo == null)
            {
                pathReferenceCache = FindAllPathReferences();
            }
            else if (!pipeline.ExecutionInfo.TryGetValue(pathReferenceCacheKey, out pathReferenceCache))
            {
                pipeline.ExecutionInfo[pathReferenceCacheKey] = pathReferenceCache = FindAllPathReferences(); 
            }

            var callerPath = string.Empty;
            var callerLink = string.Empty;

            if (pipeline != null && caller)
            {
                callerPath = UnityWebRequest.EscapeURL(AssetDatabase.GetAssetPath(caller));
                callerLink = $"[{pipeline.name}.{caller.name}](assetlink://{callerPath})";
            }

            var match = referenceIdentifier.Match(result);
            while (match != null && !string.IsNullOrEmpty(match.Value))
            {
                var matchValue = match.Value.Trim(opo, opc);
                if (!pathReferenceCache.TryGetValue(matchValue, out var pathReference))
                {
                    if (caller)
                    {
                        EditorGUIUtility.PingObject(caller);
                    }
                    throw new InvalidOperationException($"{callerLink} No PathReference named \"{matchValue}\" found in AssetDatabase");
                }

                var replacement = pathReference.GetPath(pipeline);
                result = result.Replace(match.Value, replacement);
                match = match.NextMatch();
            }

            return result.Replace("\\", "/");
        }

        public override Type ElementType => typeof(PathComponent);

        public override bool SupportsType(Type type) => ElementType.IsAssignableFrom(type);

        public string GetPath(Pipeline pipeline)
        {
            using (PathResolutionScope.Enter(this))
                return PathAssembler.Assemble(this, pipeline, Data);
        }

        private static Dictionary<string, PathReference> FindAllPathReferences()
        {
            var pathReferences = AssetDatabase.FindAssets($"t:{nameof(PathReference)}", Constants.FindAllFolders)
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(assetPath => AssetDatabase.LoadAssetAtPath<PathReference>(assetPath))
                .Where(pathReference => pathReference != null)
                .ToArray();

            var ambiguous = pathReferences.GroupBy(pathReference => pathReference.name)
                                          .FirstOrDefault(group => group.Count() > 1);
            if (ambiguous != null)
            {
                var assetPaths = ambiguous.Select(pathReference => AssetDatabase.GetAssetPath(pathReference)).ToArray();
                throw new InvalidOperationException(
                    $"PathReference name \"{ambiguous.Key}\" is used by more than one asset: {string.Join(", ", assetPaths)}. " +
                    "PathReference names must be unique because they are addressed by name.");
            }

            return pathReferences.ToDictionary(pathReference => pathReference.name);
        }

        public override string ElementTemplate =>
$@"using ThunderKit.Core.Pipelines;
using ThunderKit.Core.Paths;

namespace {{0}}
{{{{
    public class {{1}} : PathComponent
    {{{{
        public override string GetPath({nameof(PathReference)} output, Pipeline pipeline)
        {{{{
            return base.GetPath(output, pipeline);
        }}}}
    }}}}
}}}}
";
    }
}
