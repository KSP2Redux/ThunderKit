using System;
using System.IO;
using NUnit.Framework;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Paths.Components;
using ThunderKit.Core.Pipelines;
using UnityEditor;

namespace ThunderKitTests
{
    // Characterises what each PathComponent returns today, including the cases that
    // return null or an absolute path, because those results are combined into paths
    // that Delete, Copy and ExecuteProcess act on unchecked.
    [TestFixture]
    public class PathComponentTests
    {
        PathComponentFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new PathComponentFixture();
        }

        [TearDown]
        public void TearDown()
        {
            fixture.DestroyAll();
        }

        [Test]
        public void PathComponent_Base_ReturnsEmpty()
        {
            Assert.That(fixture.Create<PathComponent>().GetPath(null, null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ThunderKitRoot_ReturnsConstantFolderName()
        {
            Assert.That(fixture.Create<ThunderKitRoot>().GetPath(null, null), Is.EqualTo("ThunderKit"));
        }

        [Test]
        public void WorkingDirectory_ReturnsCurrentDirectory()
        {
            Assert.That(fixture.Create<WorkingDirectory>().GetPath(null, null),
                Is.EqualTo(Directory.GetCurrentDirectory()));
        }

        [Test]
        public void WorkingDirectory_IsRooted()
        {
            Assert.That(Path.IsPathRooted(fixture.Create<WorkingDirectory>().GetPath(null, null)), Is.True);
        }

        [Test]
        public void AssetReference_UnassignedAsset_ReturnsEmpty()
        {
            Assert.That(fixture.Create<AssetReference>().GetPath(null, null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void AssetReference_AssignedFolder_ReturnsProjectRelativePath()
        {
            const string folderPath = "Assets/__TK_AssetReference__";
            AssetDatabase.DeleteAsset(folderPath);
            AssetDatabase.CreateFolder("Assets", "__TK_AssetReference__");
            try
            {
                var assetReference = fixture.Create<AssetReference>();
                assetReference.Asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);

                Assert.That(assetReference.GetPath(null, null), Is.EqualTo(folderPath));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void Resolver_ValueWithoutToken_ReturnsValueWithForwardSlashes()
        {
            var resolver = fixture.Create<Resolver>();
            resolver.value = "some\\plain\\path";

            Assert.That(resolver.GetPath(null, null), Is.EqualTo("some/plain/path"));
        }

        [Test]
        public void Resolver_UnknownToken_Throws()
        {
            var resolver = fixture.Create<Resolver>();
            resolver.value = "<__TK_NonExistentReference__>";

            Assert.Throws<InvalidOperationException>(() => resolver.GetPath(null, null));
        }

        [Test]
        public void OutputReference_ResolvesTargetReference()
        {
            var target = fixture.Reference(fixture.Literal("ThunderKit"), fixture.Literal("Staging"));
            var output = fixture.Output(target);

            Assert.That(output.GetPath(null, null), Is.EqualTo(Path.Combine("ThunderKit", "Staging")));
        }

        [Test]
        public void OutputReference_UnassignedReference_ThrowsWithDiagnosticLink()
        {
            var owner = fixture.Create<PathReference>();
            var output = fixture.Create<OutputReference>();

            var exception = Assert.Throws<InvalidOperationException>(() => output.GetPath(owner, null));
            Assert.That(exception.Message, Does.Contain("unassigned or null"));
            Assert.That(exception.Message, Does.Contain("assetlink://"));
        }

        [Test]
        public void ManifestName_NoManifest_ReturnsNull()
        {
            var pipeline = fixture.Create<Pipeline>();

            Assert.That(fixture.Create<ManifestName>().GetPath(null, pipeline), Is.Null);
        }

        // The null-conditional chain short circuits, so the catch block that reports
        // "ManifestIdentity not found" is never reached for this case.
        [Test]
        public void ManifestName_ManifestWithoutIdentity_ReturnsNull()
        {
            var pipeline = fixture.Create<Pipeline>();
            pipeline.manifest = fixture.Create<ThunderKit.Core.Manifests.Manifest>();

            Assert.That(fixture.Create<ManifestName>().GetPath(null, pipeline), Is.Null);
        }

        [Test]
        public void ManifestName_NullPipeline_ThrowsNullReference()
        {
            Assert.Throws<NullReferenceException>(() => fixture.Create<ManifestName>().GetPath(null, null));
        }

        [Test]
        public void RegistryLookup_MissingKey_ReturnsNull()
        {
            if (!PathComponentFixture.DosPaths)
                Assert.Ignore("Registry lookups are Windows only.");

            var lookup = fixture.Create<RegistryLookup>();
            lookup.KeyName = @"HKEY_CURRENT_USER\Software\__TK_NonExistentKey__";
            lookup.ValueName = "Path";

            Assert.That(lookup.GetPath(null, null), Is.Null);
        }
    }
}
