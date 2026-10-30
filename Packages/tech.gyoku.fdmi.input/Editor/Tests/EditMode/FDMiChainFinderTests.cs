using System.Collections.Generic;
using FDMi.core;
using NUnit.Framework;
using UnityEngine;

namespace FDMi.input.Editor.Tests
{
    public class FDMiChainFinderTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        FDMiFloat _data;
        FDMiTransformMove _move;
        FDMiTransformRotate _rotate;

        [SetUp]
        public void SetUp()
        {
            var dataGo = New("chain-data");
            _data = dataGo.AddComponent<FDMiFloat>();
            _data.data[0] = 0.5f;

            var moveGo = New("chain-move");
            _move = moveGo.AddComponent<FDMiTransformMove>();
            _move.Value = _data;
            _move.target = moveGo.AddComponent<FDMiTransformRef>();
            _move.target.data[0] = New("chain-move-target").transform;

            var rotateGo = New("chain-rotate");
            _rotate = rotateGo.AddComponent<FDMiTransformRotate>();
            _rotate.Value = _data;
            _rotate.target = rotateGo.AddComponent<FDMiTransformRef>();
            _rotate.target.data[0] = New("chain-rotate-target").transform;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        [Test]
        public void FindMoves_ReturnsComponentsReferencingData()
        {
            List<FDMiTransformMove> moves = FDMiChainFinder.FindMoves(_data);

            Assert.That(moves, Has.Count.EqualTo(1));
            Assert.That(moves[0], Is.SameAs(_move));
        }

        [Test]
        public void FindRotates_ReturnsComponentsReferencingData()
        {
            List<FDMiTransformRotate> rotates = FDMiChainFinder.FindRotates(_data);

            Assert.That(rotates, Has.Count.EqualTo(1));
            Assert.That(rotates[0], Is.SameAs(_rotate));
        }

        [Test]
        public void FindMoves_IgnoresInactiveComponents()
        {
            _move.gameObject.SetActive(false);

            Assert.That(FDMiChainFinder.FindMoves(_data), Is.Empty);
        }

        GameObject New(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }
    }
}
