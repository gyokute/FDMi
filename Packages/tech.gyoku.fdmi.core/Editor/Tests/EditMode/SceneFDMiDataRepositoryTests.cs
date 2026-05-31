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
        public void Find_RelativePath_FindsSiblingData()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var dataGo = Create("myBool", parent.transform);
            var data = dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(data, result);
        }

        [Test]
        public void Find_RelativePath_FindsDescendantData()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var intermediate = Create("middle", parent.transform);
            var dataGo = Create("myBool", intermediate.transform);
            var data = dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(data, result);
        }

        [Test]
        public void Find_RelativePath_DoesNotCrossNamespaceBoundary()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var nsBoundary = Create("ns", parent.transform);
            AddNamespace(nsBoundary, "SomeNS");
            var dataGo = Create("myBool", nsBoundary.transform);
            dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.IsNull(result);
        }

        [Test]
        public void Find_RelativePath_NullParent_SearchesOwnChildren()
        {
            var context = Create("context"); // parent = null (シーンルート)
            var dataGo = Create("myBool", context.transform);
            var data = dataGo.AddComponent<FDMiBool>();

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("myBool"), typeof(FDMiBool));

            Assert.AreEqual(data, result);
        }

        [Test]
        public void Find_RelativePath_WrongType_ReturnsNull()
        {
            var parent = Create("parent");
            var context = Create("context", parent.transform);
            var dataGo = Create("myData", parent.transform);
            dataGo.AddComponent<FDMiBool>(); // BoolではなくFloatを期待

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("myData"), typeof(FDMiFloat));

            Assert.IsNull(result);
        }

        // --- 絶対パス（IsAbsolute = true）テスト ---

        [Test]
        public void Find_AbsolutePath_FindsDataInRootNamespace()
        {
            var nsGo = Create("NS_A");
            AddNamespace(nsGo, "NS_A", isRoot: true);
            var dataGo = Create("myBool", nsGo.transform);
            var data = dataGo.AddComponent<FDMiBool>();
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("NS_A/myBool"), typeof(FDMiBool));

            Assert.AreEqual(data, result);
        }

        [Test]
        public void Find_AbsolutePath_TraversesNestedNamespaces()
        {
            var nsAGo = Create("NS_A");
            AddNamespace(nsAGo, "NS_A", isRoot: true);
            var nsBGo = Create("NS_B", nsAGo.transform);
            AddNamespace(nsBGo, "NS_B");
            var dataGo = Create("myBool", nsBGo.transform);
            var data = dataGo.AddComponent<FDMiBool>();
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("NS_A/NS_B/myBool"), typeof(FDMiBool));

            Assert.AreEqual(data, result);
        }

        [Test]
        public void Find_AbsolutePath_RootNamespaceMissing_ReturnsNull()
        {
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("NS_MISSING/myBool"), typeof(FDMiBool));

            Assert.IsNull(result);
        }

        [Test]
        public void Find_AbsolutePath_ChildNamespaceMissing_ReturnsNull()
        {
            var nsAGo = Create("NS_A");
            AddNamespace(nsAGo, "NS_A", isRoot: true);
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("NS_A/NS_MISSING/myBool"), typeof(FDMiBool));

            Assert.IsNull(result);
        }

        [Test]
        public void Find_AbsolutePath_DataNotFoundInNamespace_ReturnsNull()
        {
            var nsGo = Create("NS_A");
            AddNamespace(nsGo, "NS_A", isRoot: true);
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse("NS_A/missing"), typeof(FDMiBool));

            Assert.IsNull(result);
        }

        [Test]
        public void Find_AbsolutePath_DoesNotCrossNestedNamespaceBoundary()
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
            var result = repo.Find(context, FDMiDataPath.Parse("NS_A/myBool"), typeof(FDMiBool));

            Assert.IsNull(result);
        }

        [Test]
        public void Find_EmptyDataName_ReturnsNull()
        {
            var context = Create("context");

            var repo = new SceneFDMiDataRepository();
            var result = repo.Find(context, FDMiDataPath.Parse(string.Empty), typeof(FDMiBool));

            Assert.IsNull(result);
        }
    }
}
