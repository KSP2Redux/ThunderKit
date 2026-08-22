using System;
using System.Reflection;
using NUnit.Framework;
using ThunderKit.Core.Paths;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThunderKitTests
{
    // ResolvePath is static. With a null pipeline it falls back to scanning the
    // AssetDatabase for PathReferences (available in EditMode), so these tests
    // avoid depending on any specific project asset: one input has no token, and
    // the other uses a deliberately non-existent token name.
    [TestFixture]
    public class PathReferenceTests
    {
        [Test]
        public void ResolvePath_WithoutTokens_NormalizesBackslashes()
        {
            var result = PathReference.ResolvePath("some\\plain\\path", null, null);
            Assert.That(result, Is.EqualTo("some/plain/path"));
        }

        [Test]
        public void ResolvePath_UnknownToken_ThrowsInvalidOperationException()
        {
            Assert.That(
                () => PathReference.ResolvePath("<__NonExistentPathReference_Test__>", null, null),
                Throws.InstanceOf<InvalidOperationException>());
        }

        // The template is the source for scaffolded PathComponents, so it has to
        // override the virtual extension point rather than the public entry point.
        [Test]
        public void ElementTemplate_OverridesTheVirtualExtensionPoint()
        {
            var reference = ScriptableObject.CreateInstance<PathReference>();
            try
            {
                var extensionPoint = typeof(PathComponent).GetMethod(
                    "GetPathInternal", BindingFlags.Instance | BindingFlags.NonPublic);
                var entryPoint = typeof(PathComponent).GetMethod(
                    "GetPath", BindingFlags.Instance | BindingFlags.Public);

                Assert.That(extensionPoint, Is.Not.Null);
                Assert.That(extensionPoint.IsVirtual, Is.True);
                Assert.That(entryPoint.IsVirtual, Is.False);

                Assert.That(reference.ElementTemplate, Does.Contain(extensionPoint.Name));
                Assert.That(reference.ElementTemplate,
                    Does.Not.Contain($"override string {entryPoint.Name}("));
            }
            finally
            {
                Object.DestroyImmediate(reference);
            }
        }
    }
}
