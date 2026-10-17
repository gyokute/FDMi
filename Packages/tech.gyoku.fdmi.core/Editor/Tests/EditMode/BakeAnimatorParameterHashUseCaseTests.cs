using System.Collections.Generic;
using FDMi.core;
using FDMi.core.Editor.Application.UseCases;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Tests
{
    // FDMiAnimationDriver 派生テスト用スタブ
    class StubAnimationDriver : FDMiAnimationDriver { }

    // ドライバ継承でないテスト用スタブ
    class StubNonDriver : MonoBehaviour { }

    public class BakeAnimatorParameterHashUseCaseTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null)
                    Object.DestroyImmediate(go);
            _created.Clear();
        }

        GameObject NewGO(string name = "go")
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        string ReadHash(int instanceId)
        {
            var comp = EditorUtility.InstanceIDToObject(instanceId) as Component;
            return new SerializedObject(comp).FindProperty("valuePathHash").intValue.ToString();
        }

        [Test]
        public void Execute_ParameterName_BakesStringToHash()
        {
            var go = NewGO();
            var driver = go.AddComponent<StubAnimationDriver>();
            var so = new SerializedObject(driver);
            so.FindProperty("parameterName").stringValue = "Rudder";
            so.ApplyModifiedPropertiesWithoutUndo();

            new BakeAnimatorParameterHashUseCase().Execute(driver);

            Assert.AreEqual(Animator.StringToHash("Rudder").ToString(), ReadHash(driver.GetInstanceID()));
        }

        [Test]
        public void Execute_EmptyParameterName_KeepsHashValue()
        {
            var go = NewGO();
            var driver = go.AddComponent<StubAnimationDriver>();
            var so = new SerializedObject(driver);
            so.FindProperty("valuePathHash").intValue = 42;
            so.ApplyModifiedPropertiesWithoutUndo();

            new BakeAnimatorParameterHashUseCase().Execute(driver);

            Assert.AreEqual("42", ReadHash(driver.GetInstanceID()));
        }

        [Test]
        public void Execute_NullTarget_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new BakeAnimatorParameterHashUseCase().Execute(null));
        }

        [Test]
        public void Execute_NonDriverTarget_DoesNotThrow()
        {
            var go = NewGO();
            var mb = go.AddComponent<StubNonDriver>();
            Assert.DoesNotThrow(() => new BakeAnimatorParameterHashUseCase().Execute(mb));
        }
    }
}
