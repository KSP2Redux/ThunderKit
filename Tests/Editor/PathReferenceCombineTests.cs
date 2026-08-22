using System;
using System.IO;
using NUnit.Framework;
using ThunderKit.Core;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Pipelines;

namespace ThunderKitTests
{
    // How PathReference turns its PathComponents into one path. Several cases pin
    // authoring patterns that ship in Templates/; the rest record what an
    // unexpected component result does today, since the assembled path is handed
    // straight to Delete, Copy, Zip and ExecuteProcess.
    //
    // A reference cycle is deliberately not covered: without a guard it recurses
    // until StackOverflowException, which cannot be caught and would take the test
    // runner down with it.
    [TestFixture]
    public class PathReferenceCombineTests
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
        public void GetPath_CombinesComponentsInOrder()
        {
            var reference = fixture.Reference(fixture.Literal("ThunderKit"), fixture.Literal("Staging"));

            Assert.That(reference.GetPath(null), Is.EqualTo(Path.Combine("ThunderKit", "Staging")));
        }

        [Test]
        public void GetPath_SingleComponent_ReturnsSegmentVerbatim()
        {
            Assert.That(fixture.Reference(fixture.Literal("ThunderKit")).GetPath(null), Is.EqualTo("ThunderKit"));
        }

        [Test]
        public void GetPath_NoComponents_ReturnsEmpty()
        {
            Assert.That(fixture.Reference().GetPath(null), Is.Empty);
        }

        [Test]
        public void GetPath_EmptySegment_IsOmitted()
        {
            var reference = fixture.Reference(
                fixture.Literal("ThunderKit"),
                fixture.Literal(string.Empty),
                fixture.Literal("Staging"));

            Assert.That(reference.GetPath(null), Is.EqualTo(Path.Combine("ThunderKit", "Staging")));
        }

        [Test]
        public void GetPath_NonPathComponentData_IsIgnored()
        {
            var reference = fixture.Create<PathReference>();
            reference.Data = new ComposableElement[]
            {
                fixture.Create<ComposableElement>(),
                fixture.Literal("Staging")
            };

            Assert.That(reference.GetPath(null), Is.EqualTo("Staging"));
        }

        // Templates/BepInEx/PathReferences/BepInExPackSource.asset is FindDirectory
        // followed by Constant(".."), so parent traversal must keep working.
        [Test]
        public void GetPath_ParentDirectorySegment_IsPreserved()
        {
            var reference = fixture.Reference(fixture.Literal("BepInExPack"), fixture.Literal(".."));

            Assert.That(reference.GetPath(null), Is.EqualTo(Path.Combine("BepInExPack", "..")));
        }

        // Templates/PathReferences/GameExecutable.asset is GamePath, which is
        // absolute, followed by the executable file name.
        [Test]
        public void GetPath_RootedFirstComponent_IsAllowed()
        {
            var reference = fixture.Reference(
                fixture.Literal(PathComponentFixture.RootedPath),
                fixture.Literal("Example.exe"));

            Assert.That(reference.GetPath(null),
                Is.EqualTo(Path.Combine(PathComponentFixture.RootedPath, "Example.exe")));
        }

        [Test]
        public void GetPath_NestedOutputReference_ResolvesTargetFirst()
        {
            var root = fixture.Reference(fixture.Literal("ThunderKit"), fixture.Literal("Staging"));
            var nested = fixture.Reference(fixture.Output(root), fixture.Literal("plugins"));

            Assert.That(nested.GetPath(null), Is.EqualTo(Path.Combine("ThunderKit", "Staging", "plugins")));
        }

        [Test]
        public void GetPath_SameReferenceUsedTwiceAsSiblings_ResolvesBoth()
        {
            var shared = fixture.Reference(fixture.Literal("Shared"));
            var reference = fixture.Reference(fixture.Output(shared), fixture.Output(shared));

            Assert.That(reference.GetPath(null), Is.EqualTo(Path.Combine("Shared", "Shared")));
        }

        // Current behaviour: a rooted segment silently discards everything before it.
        [Test]
        public void GetPath_RootedSegmentAfterFirst_DiscardsPrecedingSegments()
        {
            var reference = fixture.Reference(
                fixture.Literal("ThunderKit"),
                fixture.Literal(PathComponentFixture.RootedPath));

            Assert.That(reference.GetPath(null), Is.EqualTo(PathComponentFixture.RootedPath));
        }

        // Current behaviour: "C:" is what %SYSTEMDRIVE% and %HOMEDRIVE% expand to.
        // The exact result differs between runtimes, but the preceding segments are
        // dropped on all of them.
        [Test]
        public void GetPath_DriveRelativeSegment_DiscardsPrecedingSegments()
        {
            if (!PathComponentFixture.DosPaths)
                Assert.Ignore("Drive-relative paths are a DOS path concept.");

            var reference = fixture.Reference(fixture.Literal("ThunderKit"), fixture.Literal("BepInEx"));
            var expected = reference.GetPath(null);

            var driveRelative = fixture.Reference(fixture.Literal("ThunderKit"), fixture.Literal("C:"));
            var result = driveRelative.GetPath(null);

            Assert.That(result, Is.Not.EqualTo(expected));
            Assert.That(result, Does.Not.StartWith("ThunderKit"));
        }

        // Current behaviour: ManifestName returns null with no manifest assigned, and
        // Path.Combine rejects it without naming the component responsible.
        [Test]
        public void GetPath_ComponentReturnsNull_ThrowsArgumentNullException()
        {
            var pipeline = fixture.Create<Pipeline>();
            var reference = fixture.Reference(
                fixture.Create<ThunderKit.Core.Paths.Components.ManifestName>());

            Assert.Throws<ArgumentNullException>(() => reference.GetPath(pipeline));
        }

        // Current behaviour: a PathReference that has never had components assigned
        // dereferences a null array.
        [Test]
        public void GetPath_UnassignedData_ThrowsNullReferenceException()
        {
            var reference = fixture.Create<PathReference>();

            Assert.That(reference.Data, Is.Null);
            Assert.Throws<NullReferenceException>(() => reference.GetPath(null));
        }
    }
}
