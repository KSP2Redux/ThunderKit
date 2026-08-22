using NUnit.Framework;
using ThunderKit.Core.Paths.Components;
using UnityEngine;

namespace ThunderKitTests
{
    [TestFixture]
    public class ConstantTests
    {
        Constant constant;

        [SetUp]
        public void SetUp()
        {
            // Constant.GetPathInternal ignores the output/pipeline arguments, so nulls
            // are fine for every case here.
            constant = ScriptableObject.CreateInstance<Constant>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(constant);
        }

        [Test]
        public void GetPath_ReturnsConfiguredValue()
        {
            constant.Value = "MyConstantPath";

            Assert.That(constant.GetPath(null, null), Is.EqualTo("MyConstantPath"));
        }

        [Test]
        public void GetPath_EmptyValue_ReturnsEmpty()
        {
            constant.Value = string.Empty;

            Assert.That(constant.GetPath(null, null), Is.Empty);
        }

        [Test]
        public void GetPath_ValueWithSeparators_IsNotNormalised()
        {
            constant.Value = "a\b/c";

            Assert.That(constant.GetPath(null, null), Is.EqualTo("a\b/c"));
        }

        // Current behaviour: Constant is literal. Pull request #132 proposes expanding
        // environment variables here, which this case would detect.
        [Test]
        public void GetPath_EnvironmentVariableSyntax_IsNotExpanded()
        {
            constant.Value = "%APPDATA%";

            Assert.That(constant.GetPath(null, null), Is.EqualTo("%APPDATA%"));
        }

        [Test]
        public void GetPath_PathReferenceTokenSyntax_IsNotResolved()
        {
            constant.Value = "<StagingRoot>";

            Assert.That(constant.GetPath(null, null), Is.EqualTo("<StagingRoot>"));
        }
    }
}
