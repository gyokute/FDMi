using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FDMi.core.Editor.Application.UseCases;
using FDMi.core.Attributes;
using FDMi.core.Editor.Domain.Entities;
using FDMi.core.Editor.Domain.Repositories;

namespace FDMi.core.Editor.Tests
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

    // テスト用 MonoBehaviour: [FDMiDataPath] 付きフィールドあり（ペアの文字列フィールドを持つ）
    class BehaviourWithDataPath : MonoBehaviour
    {
        public string targetFieldPath = "myData";

        [FDMiDataPathAttribute(nameof(targetFieldPath))]
        public FDMiData targetField;
    }

    // テスト用 MonoBehaviour: [FDMiDataPath] 付きフィールドなし
    class BehaviourWithoutDataPath : MonoBehaviour
    {
        public FDMiData notAnnotated;
    }

    // テスト用 MonoBehaviour: [FDMiDataPath] 付き配列フィールドあり（ペアの文字列フィールドを持つ）
    class BehaviourWithArrayDataPath : MonoBehaviour
    {
        public string targetArrayPath = "sample";

        [FDMiDataPathAttribute(nameof(targetArrayPath))]
        public FDMiBool[] targetArray;
    }

    // テスト用 MonoBehaviour: 属性が指すペアフィールドが存在しない
    class BehaviourWithMissingPairField : MonoBehaviour
    {
        [FDMiDataPathAttribute("doesNotExist")]
        public FDMiData targetField;
    }

    // テスト用 MonoBehaviour: 属性が指すペアフィールドが string 型でない
    class BehaviourWithNonStringPairField : MonoBehaviour
    {
        public int notAPathString = 42;

        [FDMiDataPathAttribute(nameof(notAPathString))]
        public FDMiData targetField;
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
        public void Execute_RepositoryReturnsEmpty_FieldRemainsNull()
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
        public void Execute_ParsesPathFromPairedStringField()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithDataPath>();
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(1, stub.ReceivedPaths.Count);
            Assert.AreEqual("myData", stub.ReceivedPaths[0].DataName);
        }

        [Test]
        public void Execute_ArrayField_MultipleMatches_AssignsAllToArray()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithArrayDataPath>();
            var dataGoA = NewGO("dataA");
            var dataA = dataGoA.AddComponent<FDMiBool>();
            var dataGoB = NewGO("dataB");
            var dataB = dataGoB.AddComponent<FDMiBool>();

            var stub = new StubRepository { ReturnValues = new FDMiData[] { dataA, dataB } };
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(2, mb.targetArray.Length);
            Assert.AreEqual(dataA, mb.targetArray[0]);
            Assert.AreEqual(dataB, mb.targetArray[1]);
        }

        [Test]
        public void Execute_ArrayField_SingleMatch_AssignsSingleElementArray()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithArrayDataPath>();
            var dataGo = NewGO("dataA");
            var data = dataGo.AddComponent<FDMiBool>();

            var stub = new StubRepository { ReturnValues = new FDMiData[] { data } };
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(1, mb.targetArray.Length);
            Assert.AreEqual(data, mb.targetArray[0]);
        }

        [Test]
        public void Execute_ArrayField_NoMatches_LeavesArrayUnchanged()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithArrayDataPath>();
            var dataGo = NewGO("previous");
            var previous = dataGo.AddComponent<FDMiBool>();
            mb.targetArray = new[] { previous };

            var stub = new StubRepository(); // ReturnValues = 空配列
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(1, mb.targetArray.Length);
            Assert.AreEqual(previous, mb.targetArray[0]);
        }

        [Test]
        public void Execute_NonArrayField_MultipleMatches_AssignsFirst()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithDataPath>();
            var dataGoA = NewGO("dataA");
            var dataA = dataGoA.AddComponent<FDMiBool>();
            var dataGoB = NewGO("dataB");
            var dataB = dataGoB.AddComponent<FDMiBool>();

            var stub = new StubRepository { ReturnValues = new FDMiData[] { dataA, dataB } };
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(dataA, mb.targetField);
        }

        [Test]
        public void Execute_PathFieldValuePerInstance_ResolvesUsingCurrentValue()
        {
            var goA = NewGO("a");
            var mbA = goA.AddComponent<BehaviourWithDataPath>();
            mbA.targetFieldPath = "pathA";

            var goB = NewGO("b");
            var mbB = goB.AddComponent<BehaviourWithDataPath>();
            mbB.targetFieldPath = "pathB";

            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mbA);
            useCase.Execute(mbB);

            Assert.AreEqual(2, stub.ReceivedPaths.Count);
            Assert.AreEqual("pathA", stub.ReceivedPaths[0].DataName);
            Assert.AreEqual("pathB", stub.ReceivedPaths[1].DataName);
        }

        [Test]
        public void Execute_PairedFieldDoesNotExist_DoesNotCallRepositoryAndFieldRemainsNull()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithMissingPairField>();
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(0, stub.ReceivedPaths.Count);
            Assert.IsNull(mb.targetField);
        }

        [Test]
        public void Execute_PairedFieldIsNotString_DoesNotCallRepositoryAndFieldRemainsNull()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithNonStringPairField>();
            var stub = new StubRepository();
            var useCase = new ResolveDataPathsUseCase(stub);

            useCase.Execute(mb);

            Assert.AreEqual(0, stub.ReceivedPaths.Count);
            Assert.IsNull(mb.targetField);
        }
    }
}
