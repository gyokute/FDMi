using NUnit.Framework;
using tech.gyoku.FDMi.core.Editor.Domain.Entities;

namespace tech.gyoku.FDMi.core.Editor.Tests
{
    public class FDMiDataPathTests
    {
        [Test]
        public void Parse_SingleSegment_IsRelativePath()
        {
            var path = FDMiDataPath.Parse("myBool");
            Assert.AreEqual("myBool", path.DataName);
            Assert.AreEqual(0, path.Namespaces.Count);
            Assert.IsFalse(path.IsAbsolute);
        }

        [Test]
        public void Parse_MultipleSegments_IsAbsolutePath()
        {
            var path = FDMiDataPath.Parse("NS_A/NS_B/myBool");
            Assert.AreEqual("myBool", path.DataName);
            CollectionAssert.AreEqual(new[] { "NS_A", "NS_B" }, path.Namespaces);
            Assert.IsTrue(path.IsAbsolute);
        }

        [Test]
        public void Parse_SingleSegmentWithNamespace_IsAbsolutePath()
        {
            var path = FDMiDataPath.Parse("NS_A/myBool");
            Assert.AreEqual("myBool", path.DataName);
            CollectionAssert.AreEqual(new[] { "NS_A" }, path.Namespaces);
            Assert.IsTrue(path.IsAbsolute);
        }

        [Test]
        public void Parse_Null_ReturnsEmptyPath()
        {
            var path = FDMiDataPath.Parse(null);
            Assert.AreEqual(string.Empty, path.DataName);
            Assert.IsFalse(path.IsAbsolute);
        }

        [Test]
        public void Parse_EmptyString_ReturnsEmptyPath()
        {
            var path = FDMiDataPath.Parse(string.Empty);
            Assert.AreEqual(string.Empty, path.DataName);
            Assert.IsFalse(path.IsAbsolute);
        }

        [Test]
        public void Equals_SamePaths_ReturnsTrue()
        {
            var a = FDMiDataPath.Parse("NS_A/myBool");
            var b = FDMiDataPath.Parse("NS_A/myBool");
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Equals_DifferentPaths_ReturnsFalse()
        {
            var a = FDMiDataPath.Parse("NS_A/myBool");
            var b = FDMiDataPath.Parse("NS_B/myBool");
            Assert.AreNotEqual(a, b);
        }
    }
}
