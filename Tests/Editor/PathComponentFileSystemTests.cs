using System;
using System.IO;
using NUnit.Framework;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Paths.Components;
using ThunderKit.Core.Pipelines;

namespace ThunderKitTests
{
    // FindFile and FindDirectory are the only components that touch the file system.
    // They are also the only ones that wrap failures in a diagnostic, so both the
    // success and the failure path are pinned here.
    [TestFixture]
    public class PathComponentFileSystemTests
    {
        static readonly string SearchRoot =
            Path.Combine(Directory.GetCurrentDirectory(), "Temp", "__TK_FindTests__");

        PathComponentFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new PathComponentFixture();

            if (Directory.Exists(SearchRoot))
                Directory.Delete(SearchRoot, true);

            Directory.CreateDirectory(Path.Combine(SearchRoot, "plugins", "nested"));
            File.WriteAllText(Path.Combine(SearchRoot, "target.dll"), string.Empty);
            File.WriteAllText(Path.Combine(SearchRoot, "plugins", "nested", "deep.dll"), string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            fixture.DestroyAll();

            if (Directory.Exists(SearchRoot))
                Directory.Delete(SearchRoot, true);
        }

        [Test]
        public void FindFile_MatchInSearchRoot_ReturnsExistingFilePath()
        {
            var findFile = fixture.Create<FindFile>();
            findFile.path = SearchRoot;
            findFile.searchPattern = "target.dll";
            findFile.searchOption = SearchOption.TopDirectoryOnly;

            var result = findFile.GetPath(null, null);

            Assert.That(File.Exists(result), Is.True, result);
            Assert.That(result, Does.EndWith("target.dll"));
        }

        [Test]
        public void FindFile_AllDirectories_FindsNestedMatch()
        {
            var findFile = fixture.Create<FindFile>();
            findFile.path = SearchRoot;
            findFile.searchPattern = "deep.dll";
            findFile.searchOption = SearchOption.AllDirectories;

            var result = findFile.GetPath(null, null);

            Assert.That(File.Exists(result), Is.True, result);
        }

        [Test]
        public void FindFile_TopDirectoryOnly_DoesNotFindNestedMatch()
        {
            var findFile = fixture.Create<FindFile>();
            findFile.path = SearchRoot;
            findFile.searchPattern = "deep.dll";
            findFile.searchOption = SearchOption.TopDirectoryOnly;

            Assert.Throws<NullReferenceException>(() => findFile.GetPath(null, null));
        }

        [Test]
        public void FindDirectory_MatchInSearchRoot_ReturnsExistingDirectoryPath()
        {
            var findDirectory = fixture.Create<FindDirectory>();
            findDirectory.path = SearchRoot;
            findDirectory.searchPattern = "plugins";
            findDirectory.searchOption = SearchOption.TopDirectoryOnly;

            var result = findDirectory.GetPath(null, null);

            Assert.That(Directory.Exists(result), Is.True, result);
            Assert.That(result, Does.EndWith("plugins"));
        }

        // The diagnostic these components build starts with pipeline.pipelineLink,
        // which dereferences a job array that Pipeline only populates once Execute
        // begins. Outside execution the reported failure is therefore a
        // NullReferenceException and the underlying cause is lost.
        [Test]
        public void FindDirectory_NoMatch_DiagnosticIsLostOutsidePipelineExecution()
        {
            var findDirectory = fixture.Create<FindDirectory>();
            findDirectory.path = SearchRoot;
            findDirectory.searchPattern = "__TK_NoSuchDirectory__";
            findDirectory.searchOption = SearchOption.TopDirectoryOnly;

            Assert.Throws<NullReferenceException>(
                () => findDirectory.GetPath(null, fixture.Create<Pipeline>()));
        }

        [Test]
        public void FindFile_MissingSearchRoot_DiagnosticIsLostOutsidePipelineExecution()
        {
            var findFile = fixture.Create<FindFile>();
            findFile.path = Path.Combine(SearchRoot, "__TK_NoSuchFolder__");
            findFile.searchPattern = "*.dll";
            findFile.searchOption = SearchOption.TopDirectoryOnly;

            Assert.Throws<NullReferenceException>(
                () => findFile.GetPath(fixture.Create<PathReference>(), fixture.Create<Pipeline>()));
        }
    }
}
