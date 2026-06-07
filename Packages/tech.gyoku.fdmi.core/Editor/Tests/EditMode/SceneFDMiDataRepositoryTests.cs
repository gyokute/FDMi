using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using tech.gyoku.FDMi.core.Editor.Domain.Entities;
using tech.gyoku.FDMi.core.Editor.Infrastructure.Repositories;

namespace tech.gyoku.FDMi.core.Editor.Tests
{
    public class SceneFDMiDataRepositoryTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _created.Clear();
        }

        GameObject Create(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            _created.Add(go);
            return go;
        }

        FDMiNamespace AddNamespace(GameObject go, string nsName, bool isRoot = false)
        {
            var ns = go.AddComponent<FDMiNamespace>();
            ns.nameSpace = nsName;
            ns.isNamespaceRoot = isRoot;
            return ns;
        }

        // --- 相対パス（IsAbsolute = false）テスト ---

        [Test]
        public void FindAll_RelativePath_FindsSiblingData()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var dataGo = Create("myBool", parent.transform);
            var data = dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(data, result[0]);
        }

        [Test]
        public void FindAll_RelativePath_FindsDescendantData()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var intermediate = Create("middle", parent.transform);
            var dataGo = Create("myBool", intermediate.transform);
            var data = dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(data, result[0]);
        }

        [Test]
        public void FindAll_RelativePath_DoesNotCrossNamespaceBoundary()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var nsBoundary = Create("ns", parent.transform);
            AddNamespace(nsBoundary, "SomeNS");
            var dataGo = Create("myBool", nsBoundary.transform);
            dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void FindAll_RelativePath_NullParent_SearchesOwnChildren()
        {
            var context = Create("context"); // parent = null (シーンルート)
            var dataGo = Create("myBool", context.transform);
            var data = dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(data, result[0]);
        }

        [Test]
        public void FindAll_RelativePath_WrongType_ReturnsEmpty()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var dataGo = Create("myData", parent.transform);
            dataGo.AddComponent<FDMiBool>(); // BoolではなくFloatを期待

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("myData"), typeof(FDMiFloat));

            Assert.AreEqual(0, result.Length);
        }

        // --- 絶対パス（IsAbsolute = true）テスト ---

        [Test]
        public void FindAll_AbsolutePath_FindsDataInRootNamespace()
        {
            var nsGo = Create("NS_A");
            AddNamespace(nsGo, "NS_A", isRoot: true);
            var dataGo = Create("myBool", nsGo.transform);
            var data = dataGo.AddComponent<FDMiBool>();
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_A/myBool"), typeof(FDMiBool));

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(data, result[0]);
        }

        [Test]
        public void FindAll_AbsolutePath_TraversesNestedNamespaces()
        {
            var nsAGo = Create("NS_A");
            AddNamespace(nsAGo, "NS_A", isRoot: true);
            var nsBGo = Create("NS_B", nsAGo.transform);
            AddNamespace(nsBGo, "NS_B");
            var dataGo = Create("myBool", nsBGo.transform);
            var data = dataGo.AddComponent<FDMiBool>();
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_A/NS_B/myBool"), typeof(FDMiBool));

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(data, result[0]);
        }

        [Test]
        public void FindAll_AbsolutePath_RootNamespaceMissing_ReturnsEmpty()
        {
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_MISSING/myBool"), typeof(FDMiBool));

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void FindAll_AbsolutePath_ChildNamespaceMissing_ReturnsEmpty()
        {
            var nsAGo = Create("NS_A");
            AddNamespace(nsAGo, "NS_A", isRoot: true);
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_A/NS_MISSING/myBool"), typeof(FDMiBool));

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void FindAll_AbsolutePath_DataNotFoundInNamespace_ReturnsEmpty()
        {
            var nsGo = Create("NS_A");
            AddNamespace(nsGo, "NS_A", isRoot: true);
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_A/missing"), typeof(FDMiBool));

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void FindAll_AbsolutePath_DoesNotCrossNestedNamespaceBoundary()
        {
            var nsAGo = Create("NS_A");
            AddNamespace(nsAGo, "NS_A", isRoot: true);
            var nsCGo = Create("NS_C", nsAGo.transform);
            AddNamespace(nsCGo, "NS_C"); // 別のNS
            var dataGo = Create("myBool", nsCGo.transform); // NS_C の中
            dataGo.AddComponent<FDMiBool>();
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            // NS_A/myBool を探す。NS_C の中は境界を越えるので見つからないはず
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_A/myBool"), typeof(FDMiBool));

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void FindAll_RelativePath_MultipleMatches_ReturnsAllInScope()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var dataGoA = Create("sample", parent.transform);
            var dataA = dataGoA.AddComponent<FDMiBool>();
            var middle = Create("middle", parent.transform);
            var dataGoB = Create("sample", middle.transform);
            var dataB = dataGoB.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("sample"), typeof(FDMiBool));

            Assert.AreEqual(2, result.Length);
            CollectionAssert.Contains(result, dataA);
            CollectionAssert.Contains(result, dataB);
        }

        [Test]
        public void FindAll_AbsolutePath_MultipleMatchesInNamespace_ReturnsAll()
        {
            var nsGo = Create("NS_A");
            AddNamespace(nsGo, "NS_A", isRoot: true);
            var dataGoA = Create("sample", nsGo.transform);
            var dataA = dataGoA.AddComponent<FDMiBool>();
            var middle = Create("middle", nsGo.transform);
            var dataGoB = Create("sample", middle.transform);
            var dataB = dataGoB.AddComponent<FDMiBool>();
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse("NS_A/sample"), typeof(FDMiBool));

            Assert.AreEqual(2, result.Length);
            CollectionAssert.Contains(result, dataA);
            CollectionAssert.Contains(result, dataB);
        }

        [Test]
        public void FindAll_EmptyDataName_ReturnsEmpty()
        {
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.FindAll(context, FDMiDataPath.Parse(string.Empty), typeof(FDMiBool));

            Assert.AreEqual(0, result.Length);
        }
    }
}
