using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using tech.gyoku.FDMi.core.Editor.Application.UseCases;
using tech.gyoku.FDMi.core.Attributes;
using tech.gyoku.FDMi.core.Editor.Domain.Entities;
using tech.gyoku.FDMi.core.Editor.Domain.Repositories;

namespace tech.gyoku.FDMi.core.Editor.Tests
{
    // テスト用スタブ: IFDMiDataRepository の手書きモック
    class StubRepository : IFDMiDataRepository
    {
        public FDMiData[] ReturnValues = new FDMiData[0];
        public List<FDMiDataPath> ReceivedPaths = new List<FDMiDataPath>();

        public FDMiData[] FindAll(GameObject context, FDMiDataPath path, Type fieldType)
        {
            ReceivedPaths.Add(path);
            return ReturnValues;
        }
    }

    // テスト用 MonoBehaviour: [FDMiDataPath] 付きフィールドあり
    class BehaviourWithDataPath : MonoBehaviour
    {
        [FDMiDataPathAttribute("myData")]
        public FDMiData targetField;
    }

    // テスト用 MonoBehaviour: [FDMiDataPath] 付きフィールドなし
    class BehaviourWithoutDataPath : MonoBehaviour
    {
        public FDMiData notAnnotated;
    }

    public class ResolveDataPathsUseCaseTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [SetUp]
        public void SetUp() => ResolveDataPathsUseCase.ClearCacheForTesting();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _created.Clear();
        }

        GameObject NewGO(string name = "go")
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        [Test]
        public void Execute_NullTarget_DoesNotThrow()
        {
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);
            Assert.DoesNotThrow(() => useCase.Execute(null));
        }

        [Test]
        public void Execute_NoAnnotatedFields_DoesNotCallRepository()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithoutDataPath>();
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(0, stub.ReceivedPaths.Count);
        }

        [Test]
        public void Execute_RepositoryReturnsNull_FieldRemainsNull()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithDataPath>();
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.IsNull(mb.targetField);
        }

        [Test]
        public void Execute_RepositoryReturnsData_FieldIsAssigned()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithDataPath>();
            var dataGo = NewGO("dataObj");
            var data = dataGo.AddComponent<FDMiBool>();

            var stub = new StubRepository { ReturnValues = new FDMiData[] { data } };
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(data, mb.targetField);
        }

        [Test]
        public void Execute_ParsesPathFromAttribute()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithDataPath>();
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(1, stub.ReceivedPaths.Count);
            Assert.AreEqual("myData", stub.ReceivedPaths[0].DataName);
        }
    }
}
