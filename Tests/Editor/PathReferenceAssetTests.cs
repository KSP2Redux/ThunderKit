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

        // Current behaviour: names are collected into a dictionary, so two assets
        // sharing a name break every path resolution in the project with an
        // exception that names neither the reference nor the assets involved.
        [Test]
        public void ResolvePath_DuplicateReferenceNames_ThrowsArgumentException()
        {
            CreateReference(FirstFolder, "__TK_Duplicate__", "First");
            CreateReference(SecondFolder, "__TK_Duplicate__", "Second");

            Assert.That(() => PathReference.ResolvePath("<__TK_Duplicate__>", null, null),
                Throws.InstanceOf<ArgumentException>());
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
