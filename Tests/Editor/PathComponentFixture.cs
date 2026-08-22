using System.Collections.Generic;
using System.IO;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Paths.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThunderKitTests
{
    // Builds PathComponents and PathReferences in memory. Data is assigned directly
    // rather than through InsertElement, which would require an asset on disk.
    internal sealed class PathComponentFixture
    {
        readonly List<Object> created = new List<Object>();

        public PathReference Reference(params PathComponent[] components)
        {
            return Compose(Create<PathReference>(), components);
        }

        public PathReference Compose(PathReference reference, params PathComponent[] components)
        {
            reference.Data = components;
            return reference;
        }

        public Constant Literal(string value)
        {
            var constant = Create<Constant>();
            constant.Value = value;
            return constant;
        }

        public OutputReference Output(PathReference target)
        {
            var output = Create<OutputReference>();
            output.reference = target;
            return output;
        }

        public T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            created.Add(instance);
            return instance;
        }

        public void DestroyAll()
        {
            for (int index = created.Count - 1; index >= 0; index--)
                if (created[index])
                    Object.DestroyImmediate(created[index]);

            created.Clear();
        }

        public static bool DosPaths
        {
            get { return Path.DirectorySeparatorChar == '\'; }
        }

        // A rooted path that is valid on the platform running the tests.
        public static string RootedPath
        {
            get { return DosPaths ? @"C:\Games\Example" : "/games/example"; }
        }
    }
}
