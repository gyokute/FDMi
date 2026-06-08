using NUnit.Framework;
using FDMi.core.Editor.Domain.Entities;

namespace FDMi.core.Editor.Tests
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

        [Test]
        public void Matches_LiteralExactChain_ReturnsTrue()
        {
            var path = FDMiDataPath.Parse("NS_A/NS_B/myBool");
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "NS_A", "NS_B" }));
        }

        [Test]
        public void Matches_LiteralMismatch_ReturnsFalse()
        {
            var path = FDMiDataPath.Parse("NS_A/NS_B/myBool");
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A", "NS_X" }));
        }

        [Test]
        public void Matches_LiteralLengthMismatch_ReturnsFalse()
        {
            var path = FDMiDataPath.Parse("NS_A/NS_B/myBool");
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A", "NS_B", "NS_C" }));
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A" }));
        }

        [Test]
        public void Matches_SingleWildcard_MatchesExactlyOneSegment()
        {
            var path = FDMiDataPath.Parse("NS_A/*/NS_C/myBool");
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "NS_A", "anything", "NS_C" }));
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A", "NS_C" }));
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A", "X", "Y", "NS_C" }));
        }

        [Test]
        public void Matches_DoubleWildcard_MatchesZeroSegments()
        {
            var path = FDMiDataPath.Parse("NS_A/**/NS_C/myBool");
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "NS_A", "NS_C" }));
        }

        [Test]
        public void Matches_DoubleWildcard_MatchesMultipleSegments()
        {
            var path = FDMiDataPath.Parse("NS_A/**/NS_C/myBool");
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "NS_A", "X", "Y", "NS_C" }));
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A", "X", "NS_OTHER" }));
        }

        [Test]
        public void Matches_DoubleWildcardAlone_MatchesAnyChainIncludingEmpty()
        {
            var path = FDMiDataPath.Parse("**/myBool");
            Assert.IsTrue(path.MatchesNamespaceChain(new string[0]));
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "A" }));
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "A", "B", "C" }));
        }

        [Test]
        public void Matches_PartialWildcardSegment_TreatedAsLiteral()
        {
            var path = FDMiDataPath.Parse("NS_*/myBool");
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A" }));
            Assert.IsTrue(path.MatchesNamespaceChain(new[] { "NS_*" }));
        }

        [Test]
        public void Matches_RelativePath_EmptyPatternMatchesOnlyEmptyChain()
        {
            var path = FDMiDataPath.Parse("myBool");
            Assert.IsTrue(path.MatchesNamespaceChain(new string[0]));
            Assert.IsFalse(path.MatchesNamespaceChain(new[] { "NS_A" }));
        }
    }
}
