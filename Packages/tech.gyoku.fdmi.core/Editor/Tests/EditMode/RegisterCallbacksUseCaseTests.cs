using NUnit.Framework;
using UnityEngine;
using FDMi.core.Editor.Application.UseCases;

namespace FDMi.core.Editor.Tests
{
    // [FDMiRegisterCallback] 付きフィールド 1 つ
    class BehaviourWithRegisterCallback : MonoBehaviour
    {
        [FDMiRegisterCallback("OnChanged")]
        public FDMiBool myData;
        public void OnChanged() { }
    }

    // [FDMiRegisterCallback] なし
    class BehaviourWithoutRegisterCallback : MonoBehaviour
    {
        public FDMiBool myData;
    }

    // 同一フィールドに複数 [FDMiRegisterCallback]
    class BehaviourWithMultipleCallbacks : MonoBehaviour
    {
        [FDMiRegisterCallback("OnChangedA")]
        [FDMiRegisterCallback("OnChangedB")]
        public FDMiBool myData;
    }

    public class RegisterCallbacksUseCaseTests
    {
        readonly System.Collections.Generic.List<GameObject> _created
            = new System.Collections.Generic.List<GameObject>();

        [SetUp]
        public void SetUp() => RegisterCallbacksUseCase.ClearCacheForTesting();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        GameObject NewGO(string name = "go")
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        [Test]
        public void Attribute_HasCorrectFunctionName()
        {
            var attr = new FDMiRegisterCallbackAttribute("OnChanged");
            Assert.AreEqual("OnChanged", attr.FunctionName);
        }

        [Test]
        public void Execute_NullTarget_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new RegisterCallbacksUseCase().Execute(null));
        }

        [Test]
        public void Execute_NoAnnotatedFields_DoesNotModifyData()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithoutRegisterCallback>();
            var dataGo = NewGO("data");
            var data = dataGo.AddComponent<FDMiBool>();
            mb.myData = data;

            new RegisterCallbacksUseCase().Execute(mb);

            Assert.AreEqual(0, data.callbackBehaviour.Length);
        }

        [Test]
        public void Execute_SingleCallback_AddsEntryToData()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithRegisterCallback>();
            var dataGo = NewGO("data");
            var data = dataGo.AddComponent<FDMiBool>();
            mb.myData = data;

            new RegisterCallbacksUseCase().Execute(mb);

            Assert.AreEqual(1, data.callbackBehaviour.Length);
            Assert.AreEqual(mb, data.callbackBehaviour[0]);
            Assert.AreEqual("OnChanged", data.callbackFunction[0]);
        }

        [Test]
        public void Execute_MultipleCallbacksOnSameField_AddsAllEntries()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithMultipleCallbacks>();
            var dataGo = NewGO("data");
            var data = dataGo.AddComponent<FDMiBool>();
            mb.myData = data;

            new RegisterCallbacksUseCase().Execute(mb);

            Assert.AreEqual(2, data.callbackBehaviour.Length);
            CollectionAssert.Contains(data.callbackFunction, "OnChangedA");
            CollectionAssert.Contains(data.callbackFunction, "OnChangedB");
        }

        [Test]
        public void Execute_DataFieldIsNull_DoesNotThrowAndAddsNoEntries()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithRegisterCallback>();
            // mb.myData は null のまま
            Assert.DoesNotThrow(() => new RegisterCallbacksUseCase().Execute(mb));
        }

        [Test]
        public void Execute_CalledTwice_NoDuplicateEntries()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithRegisterCallback>();
            var dataGo = NewGO("data");
            var data = dataGo.AddComponent<FDMiBool>();
            mb.myData = data;

            var useCase = new RegisterCallbacksUseCase();
            useCase.Execute(mb);
            useCase.Execute(mb);

            Assert.AreEqual(1, data.callbackBehaviour.Length);
            Assert.AreEqual(1, data.callbackFunction.Length, "callbackFunction も重複なし");
            Assert.AreEqual(mb,          data.callbackBehaviour[0]);
            Assert.AreEqual("OnChanged", data.callbackFunction[0]);
        }

        [Test]
        public void Execute_DataFieldChanges_RemovesFromOldAddsToNew()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithRegisterCallback>();
            var dataGoA = NewGO("dataA");
            var dataA = dataGoA.AddComponent<FDMiBool>();
            var dataGoB = NewGO("dataB");
            var dataB = dataGoB.AddComponent<FDMiBool>();

            var useCase = new RegisterCallbacksUseCase();
            mb.myData = dataA;
            useCase.Execute(mb);

            mb.myData = dataB;
            useCase.Execute(mb);

            Assert.AreEqual(0, dataA.callbackBehaviour.Length, "dataA からエントリが消えていること");
            Assert.AreEqual(0, dataA.callbackFunction.Length,  "dataA の callbackFunction からも消えていること");
            Assert.AreEqual(1, dataB.callbackBehaviour.Length, "dataB にエントリが追加されていること");
            Assert.AreEqual(mb,          dataB.callbackBehaviour[0]);
            Assert.AreEqual("OnChanged", dataB.callbackFunction[0]);
        }

        [Test]
        public void Execute_DataFieldBecomesNull_RemovesFromPreviousData()
        {
            var go = NewGO();
            var mb = go.AddComponent<BehaviourWithRegisterCallback>();
            var dataGo = NewGO("data");
            var data = dataGo.AddComponent<FDMiBool>();

            var useCase = new RegisterCallbacksUseCase();
            mb.myData = data;
            useCase.Execute(mb);
            Assert.AreEqual(1, data.callbackBehaviour.Length, "前提: 初回 Execute 後にエントリが 1 件あること");

            mb.myData = null;
            useCase.Execute(mb);

            Assert.AreEqual(0, data.callbackBehaviour.Length, "null 変更後にエントリが消えていること");
            Assert.AreEqual(0, data.callbackFunction.Length,  "null 変更後に callbackFunction からも消えていること");
        }

        [Test]
        public void RegisterAll_RemovesDestroyedComponentEntries()
        {
            var mbGo   = NewGO("mb");
            var mb     = mbGo.AddComponent<BehaviourWithRegisterCallback>();
            var dataGo = NewGO("data");
            var data   = dataGo.AddComponent<FDMiBool>();
            mb.myData  = data;

            new RegisterCallbacksUseCase().Execute(mb);
            Assert.AreEqual(1, data.callbackBehaviour.Length);

            Object.DestroyImmediate(mb); // コンポーネント削除 → Unity-null

            RegisterCallbacksUseCase.RegisterAll();

            Assert.AreEqual(0, data.callbackBehaviour.Length, "破棄されたエントリが除去されていること");
            Assert.AreEqual(0, data.callbackFunction.Length,  "callbackFunction からも除去されていること");
        }

        [Test]
        public void RegisterAll_ReRegistersLivingComponents()
        {
            var go     = NewGO();
            var mb     = go.AddComponent<BehaviourWithRegisterCallback>();
            var dataGo = NewGO("data");
            var data   = dataGo.AddComponent<FDMiBool>();
            mb.myData  = data;

            RegisterCallbacksUseCase.RegisterAll();

            Assert.AreEqual(1, data.callbackBehaviour.Length);
            Assert.AreEqual(mb,          data.callbackBehaviour[0]);
            Assert.AreEqual("OnChanged", data.callbackFunction[0]);
        }

        [Test]
        public void RegisterAll_CalledTwice_NoDuplicateEntries()
        {
            var go     = NewGO();
            var mb     = go.AddComponent<BehaviourWithRegisterCallback>();
            var dataGo = NewGO("data");
            var data   = dataGo.AddComponent<FDMiBool>();
            mb.myData  = data;

            RegisterCallbacksUseCase.RegisterAll();
            RegisterCallbacksUseCase.RegisterAll();

            Assert.AreEqual(1, data.callbackBehaviour.Length);
            Assert.AreEqual(1, data.callbackFunction.Length, "callbackFunction も重複なし");
            Assert.AreEqual(mb,          data.callbackBehaviour[0]);
            Assert.AreEqual("OnChanged", data.callbackFunction[0]);
        }
    }
}
