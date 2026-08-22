using System;
using NUnit.Framework;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Paths.Components;
using UnityEditor;
using UnityEngine;

namespace ThunderKitTests
{
    // ResolvePath addresses PathReferences by name through the AssetDatabase, so
    // these cases need assets on disk rather than in-memory instances.
    [TestFixture]
    public class PathReferenceAssetTests
    {
        const string FirstFolder = "__TK_PathRefA__";
        const string SecondFolder = "__TK_PathRefB__";

        [SetUp]
        public void SetUp()
        {
            Cleanup();
        }

        [TearDown]
        public void TearDown()
        {
            Cleanup();
        }

        [Test]
        public void ResolvePath_KnownToken_SubstitutesAssembledPath()
        {
            CreateReference(FirstFolder, "__TK_TokenTarget__", "Resolved");

            Assert.That(PathReference.ResolvePath("prefix/<__TK_TokenTarget__>/suffix", null, null),
                Is.EqualTo("prefix/Resolved/suffix"));
        }

        [Test]
        public void ResolvePath_MultipleTokens_SubstitutesEach()
        {
            CreateReference(FirstFolder, "__TK_TokenOne__", "One");
            CreateReference(SecondFolder, "__TK_TokenTwo__", "Two");

            Assert.That(PathReference.ResolvePath("<__TK_TokenOne__>/<__TK_TokenTwo__>", null, null),
                Is.EqualTo("One/Two"));
        }

        // References are addressed by name, so two assets sharing one is ambiguous.
        // The failure names the duplicate and both assets rather than surfacing as a
        // bare dictionary key collision.
        [Test]
        public void ResolvePath_DuplicateReferenceNames_ReportsTheConflict()
        {
            CreateReference(FirstFolder, "__TK_Duplicate__", "First");
            CreateReference(SecondFolder, "__TK_Duplicate__", "Second");

            var exception = Assert.Throws<InvalidOperationException>(
                () => PathReference.ResolvePath("<__TK_Duplicate__>", null, null));

            Assert.That(exception.Message, Does.Contain("__TK_Duplicate__"));
            Assert.That(exception.Message, Does.Contain(FirstFolder));
            Assert.That(exception.Message, Does.Contain(SecondFolder));
        }

        // The second way resolution re-enters itself: a Resolver whose token names
        // the reference that owns it.
        [Test]
        public void GetPath_ResolverTokenNamingItsOwnReference_Throws()
        {
            var reference = CreateResolverReference(FirstFolder, "__TK_TokenCycle__");

            var exception = Assert.Throws<InvalidOperationException>(() => reference.GetPath(null));
            Assert.That(exception.Message, Does.Contain("cycle"));
        }

        static PathReference CreateResolverReference(string folder, string referenceName)
        {
            if (!AssetDatabase.IsValidFolder($"Assets/{folder}"))
                AssetDatabase.CreateFolder("Assets", folder);

            var reference = ScriptableObject.CreateInstance<PathReference>();
            AssetDatabase.CreateAsset(reference, $"Assets/{folder}/{referenceName}.asset");

            var resolver = ScriptableObject.CreateInstance<Resolver>();
            reference.InsertElement(resolver, 0);
            resolver.value = $"<{referenceName}>";

            AssetDatabase.SaveAssets();
            return reference;
        }

        static void CreateReference(string folder, string referenceName, string constantValue)
        {
            if (!AssetDatabase.IsValidFolder($"Assets/{folder}"))
                AssetDatabase.CreateFolder("Assets", folder);

            var reference = ScriptableObject.CreateInstance<PathReference>();
            AssetDatabase.CreateAsset(reference, $"Assets/{folder}/{referenceName}.asset");

            var constant = ScriptableObject.CreateInstance<Constant>();
            reference.InsertElement(constant, 0);
            constant.Value = constantValue;

            AssetDatabase.SaveAssets();
        }

        static void Cleanup()
        {
            AssetDatabase.DeleteAsset($"Assets/{FirstFolder}");
            AssetDatabase.DeleteAsset($"Assets/{SecondFolder}");
        }
    }
}
