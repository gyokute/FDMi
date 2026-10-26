using System.Collections.Generic;
using FDMi.core;
using FDMi.sync.Editor.Application.UseCases;
using NUnit.Framework;
using UnityEngine;

namespace FDMi.sync.Editor.Tests
{
    public class FillInPlaneBehavioursUseCaseTests
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

        GameObject NewGO(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        GameObject NewChild(GameObject parent, string name)
        {
            var go = NewGO(name);
            go.transform.SetParent(parent.transform);
            return go;
        }

        // 仕様: 選択時に "~" (最近傍の namespace ルート) 配下の全 FDMiBehaviour が、
        // ネストした名前空間・非アクティブ・自身を含めて階層パス順に inPlaneBehaviours へ自動解決される。
        // 別ルート配下とルートを持たない behaviour は含まない。
        [Test]
        public void Execute_名前空間ルート配下の全FDMiBehaviourを階層パス順に解決する()
        {
            var rootA = NewGO("RootA");
            rootA.AddComponent<FDMiNamespace>().isNamespaceRoot = true;

            var manager = NewChild(rootA, "Manager").AddComponent<FDMiBodyKinematicManagement>();
            NewChild(rootA, "BehaveB").AddComponent<FDMiBodyKinematicManagement>();

            var sub = NewChild(rootA, "Sub");
            sub.AddComponent<FDMiNamespace>().nameSpace = "Sub";
            NewChild(sub, "BehaveDeep").AddComponent<FDMiBodyKinematicManagement>();

            var inactive = NewChild(rootA, "BehaveInactive");
            inactive.SetActive(false);
            inactive.AddComponent<FDMiBodyKinematicManagement>();

            var rootB = NewGO("RootB");
            rootB.AddComponent<FDMiNamespace>().isNamespaceRoot = true;
            NewChild(rootB, "BehaveOther").AddComponent<FDMiBodyKinematicManagement>();
            NewGO("BehaveOrphan").AddComponent<FDMiBodyKinematicManagement>();

            new FillInPlaneBehavioursUseCase().Execute(manager);

            Assert.AreEqual(4, manager.inPlaneBehaviours.Length);
            Assert.AreEqual("BehaveB", manager.inPlaneBehaviours[0].name);
            Assert.AreEqual("BehaveInactive", manager.inPlaneBehaviours[1].name);
            Assert.AreEqual("Manager", manager.inPlaneBehaviours[2].name);
            Assert.AreEqual("BehaveDeep", manager.inPlaneBehaviours[3].name);
        }

        // 仕様: namespace ルートを持たない (= "~" の起点がない) 場合は既存の inPlaneBehaviours を変更しない。
        [Test]
        public void Execute_名前空間ルートを持たない場合は既存の配列を維持する()
        {
            var manager = NewGO("Manager").AddComponent<FDMiBodyKinematicManagement>();
            var keep = NewGO("Keep").AddComponent<FDMiBodyKinematicManagement>();
            manager.inPlaneBehaviours = new[] { keep };

            new FillInPlaneBehavioursUseCase().Execute(manager);

            CollectionAssert.AreEqual(new[] { keep }, manager.inPlaneBehaviours);
        }
    }
}
