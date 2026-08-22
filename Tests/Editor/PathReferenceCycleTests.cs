using System;
using System.IO;
using NUnit.Framework;
using ThunderKit.Core.Paths;

namespace ThunderKitTests
{
    // Resolution re-enters itself through OutputReference, so an authoring cycle
    // used to recurse until StackOverflowException, which cannot be caught and
    // takes the Editor with it. These cases are only writable once a guard exists.
    [TestFixture]
    public class PathReferenceCycleTests
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
        public void GetPath_SelfReferencingOutputReference_Throws()
        {
            var reference = fixture.Reference();
            fixture.Compose(reference, fixture.Output(reference));

            var exception = Assert.Throws<InvalidOperationException>(() => reference.GetPath(null));
            Assert.That(DescribesCycle(exception), Is.True, exception.ToString());
        }

        [Test]
        public void GetPath_MutuallyReferencingOutputReferences_Throws()
        {
            var first = fixture.Reference();
            var second = fixture.Reference();
            fixture.Compose(first, fixture.Output(second));
            fixture.Compose(second, fixture.Output(first));

            var exception = Assert.Throws<InvalidOperationException>(() => first.GetPath(null));
            Assert.That(DescribesCycle(exception), Is.True, exception.ToString());
        }

        [Test]
        public void GetPath_ThreeReferenceCycle_Throws()
        {
            var first = fixture.Reference();
            var second = fixture.Reference();
            var third = fixture.Reference();
            fixture.Compose(first, fixture.Output(second));
            fixture.Compose(second, fixture.Output(third));
            fixture.Compose(third, fixture.Output(first));

            var exception = Assert.Throws<InvalidOperationException>(() => first.GetPath(null));
            Assert.That(DescribesCycle(exception), Is.True, exception.ToString());
        }

        // A leaked resolution scope would poison every later resolution in the
        // session, so both failure kinds have to unwind cleanly.
        [Test]
        public void GetPath_AfterCycleFailure_UnrelatedReferenceStillResolves()
        {
            var cyclic = fixture.Reference();
            fixture.Compose(cyclic, fixture.Output(cyclic));
            Assert.Throws<InvalidOperationException>(() => cyclic.GetPath(null));

            AssertHealthyReferenceResolves();
        }

        [Test]
        public void GetPath_AfterValidationFailure_UnrelatedReferenceStillResolves()
        {
            var invalid = fixture.Reference(
                fixture.Literal("ThunderKit"),
                fixture.Literal(PathComponentFixture.RootedPath));
            Assert.Throws<InvalidOperationException>(() => invalid.GetPath(null));

            AssertHealthyReferenceResolves();
        }

        [Test]
        public void GetPath_SameReferenceResolvedTwiceInSequence_Succeeds()
        {
            var reference = fixture.Reference(fixture.Literal("ThunderKit"));

            Assert.That(reference.GetPath(null), Is.EqualTo("ThunderKit"));
            Assert.That(reference.GetPath(null), Is.EqualTo("ThunderKit"));
        }

        void AssertHealthyReferenceResolves()
        {
            var healthy = fixture.Reference(fixture.Literal("ThunderKit"), fixture.Literal("Staging"));

            Assert.That(healthy.GetPath(null), Is.EqualTo(Path.Combine("ThunderKit", "Staging")));
        }

        static bool DescribesCycle(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
                if (current.Message.IndexOf("cycle", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            return false;
        }
    }
}
