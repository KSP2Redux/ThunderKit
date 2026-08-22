using System;
using System.IO;
using NUnit.Framework;
using ThunderKit.Core.Paths.Components;

namespace ThunderKitTests
{
    // Both components take a name or an enum rather than a template string, so no
    // platform specific syntax is parsed and the same asset resolves on Windows,
    // Linux and macOS. CI runs these on Linux.
    [TestFixture]
    public class PathComponentEnvironmentTests
    {
        const string VariableName = "__TK_TEST_PATH_VAR__";

        PathComponentFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new PathComponentFixture();
            Environment.SetEnvironmentVariable(VariableName, null);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(VariableName, null);
            fixture.DestroyAll();
        }

        [Test]
        public void EnvironmentVariable_SetVariable_ReturnsValue()
        {
            Environment.SetEnvironmentVariable(VariableName, "SomeValue");

            Assert.That(Variable(VariableName).GetPath(null, null), Is.EqualTo("SomeValue"));
        }

        // The failure this component exists to prevent: an unset variable must not
        // become a literal path segment.
        [Test]
        public void EnvironmentVariable_UnsetVariable_ThrowsNamingTheComponent()
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => Variable(VariableName).GetPath(null, null));

            Assert.That(exception.Message, Does.Contain(VariableName));
            Assert.That(exception.Message, Does.Contain("EnvironmentVariable"));
        }

        [Test]
        public void EnvironmentVariable_UnsetVariableWithFallback_ReturnsFallback()
        {
            var component = Variable(VariableName);
            component.Fallback = "DefaultLocation";

            Assert.That(component.GetPath(null, null), Is.EqualTo("DefaultLocation"));
        }

        [Test]
        public void EnvironmentVariable_EmptyValue_IsTreatedAsUnset()
        {
            Environment.SetEnvironmentVariable(VariableName, string.Empty);

            Assert.Throws<InvalidOperationException>(() => Variable(VariableName).GetPath(null, null));
        }

        [Test]
        public void EnvironmentVariable_NoVariableName_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Variable(string.Empty).GetPath(null, null));
        }

        // The name is looked up verbatim, so shell syntax is not accepted and cannot
        // silently pass through as a literal segment.
        [Test]
        public void EnvironmentVariable_PercentSyntaxInName_IsNotUnwrapped()
        {
            Environment.SetEnvironmentVariable(VariableName, "SomeValue");

            Assert.Throws<InvalidOperationException>(
                () => Variable($"%{VariableName}%").GetPath(null, null));
        }

        [Test]
        public void SpecialFolder_DefaultsToApplicationData()
        {
            Assert.That(fixture.Create<SpecialFolder>().Folder,
                Is.EqualTo(Environment.SpecialFolder.ApplicationData));
        }

        [Test]
        public void SpecialFolder_UserProfile_ReturnsRootedPath()
        {
            AssertResolvesToRootedPath(Environment.SpecialFolder.UserProfile);
        }

        [Test]
        public void SpecialFolder_ApplicationData_ReturnsRootedPath()
        {
            AssertResolvesToRootedPath(Environment.SpecialFolder.ApplicationData);
        }

        [Test]
        public void SpecialFolder_UnknownFolder_Throws()
        {
            var component = fixture.Create<SpecialFolder>();
            component.Folder = (Environment.SpecialFolder)(-1);

            Assert.Throws<InvalidOperationException>(() => component.GetPath(null, null));
        }

        void AssertResolvesToRootedPath(Environment.SpecialFolder folder)
        {
            var component = fixture.Create<SpecialFolder>();
            component.Folder = folder;

            var result = component.GetPath(null, null);

            Assert.That(result, Is.Not.Empty);
            Assert.That(Path.IsPathRooted(result), Is.True, result);
        }

        EnvironmentVariable Variable(string variableName)
        {
            var component = fixture.Create<EnvironmentVariable>();
            component.VariableName = variableName;
            return component;
        }
    }
}
