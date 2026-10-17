using System.Collections.Generic;
using FDMi.core.Editor.Application.UseCases;
using NUnit.Framework;
using UnityEngine;

namespace FDMi.core.Editor.Tests
{
    public class FillAllSeatsUseCaseTests
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

        // 仕様: 選択時に同一名前空間ルート配下の FDMiSeat (非アクティブ含む) が階層パス順で allSeats に自動解決される。
        // 除外条件: 別クラスタの seat / namespace ルートを持たない seat。
        // 階層パス辞書順 (ordinal): "RootA/SeatB" < "RootA/SeatC" < "RootA/Sub/SeatA"
        [Test]
        public void Execute_FillsAllSeatsWithSameAnchorSeats_SortedByHierarchyPath()
        {
            var rootA = NewGO("RootA");
            rootA.AddComponent<FDMiNamespace>().isNamespaceRoot = true;
            var manager = NewChild(rootA, "Manager").AddComponent<FDMiSeatManager>();

            var seatDeep = NewChild(NewChild(rootA, "Sub"), "SeatA");
            seatDeep.AddComponent<FDMiSeat>();
            NewChild(rootA, "SeatB").AddComponent<FDMiSeat>();
            var seatInactive = NewChild(rootA, "SeatC");
            seatInactive.SetActive(false);
            seatInactive.AddComponent<FDMiSeat>();

            var rootB = NewGO("RootB");
            rootB.AddComponent<FDMiNamespace>().isNamespaceRoot = true;
            NewChild(rootB, "SeatOther").AddComponent<FDMiSeat>();
            NewGO("SeatOrphan").AddComponent<FDMiSeat>();

            new FillAllSeatsUseCase().Execute(manager);

            Assert.AreEqual(3, manager.allSeats.Length);
            Assert.AreEqual("SeatB", manager.allSeats[0].name);
            Assert.AreEqual("SeatC", manager.allSeats[1].name);
            Assert.AreEqual("SeatA", manager.allSeats[2].name);
        }
    }
}
