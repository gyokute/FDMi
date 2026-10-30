using FDMi.core.Editor.Preview;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Tests
{
    public class FDMiTransformPreviewTests
    {
        GameObject _moveGo;
        GameObject _targetGo;

        [TearDown]
        public void TearDown()
        {
            if (_moveGo != null)
                Object.DestroyImmediate(_moveGo);
            if (_targetGo != null)
                Object.DestroyImmediate(_targetGo);
        }

        [Test]
        public void CurveRange_UsesKeyTimes()
        {
            var curve = new AnimationCurve(new Keyframe(-1f, 0f), new Keyframe(2f, 1f));

            (float min, float max) = FDMiTransformPreview.CurveRange(curve);

            Assert.That(min, Is.EqualTo(-1f));
            Assert.That(max, Is.EqualTo(2f));
        }

        [Test]
        public void TryCreateMove_ReadsSerializedPositions()
        {
            _moveGo = new GameObject("preview-move");
            _targetGo = new GameObject("preview-target");

            var move = _moveGo.AddComponent<FDMiTransformMove>();
            var data = _moveGo.AddComponent<FDMiFloat>();
            var target = _moveGo.AddComponent<FDMiTransformRef>();
            data.data[0] = 0.25f;
            target.data[0] = _targetGo.transform;
            move.Value = data;
            move.target = target;
            move.outputCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

            var serializedObject = new SerializedObject(move);
            SerializedProperty position = serializedObject.FindProperty("position");
            position.arraySize = 2;
            position.GetArrayElementAtIndex(0).vector3Value = new Vector3(1f, 2f, 3f);
            position.GetArrayElementAtIndex(1).vector3Value = new Vector3(4f, 5f, 6f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            bool created = FDMiTransformPreview.TryCreateMove(
                move,
                out FDMiMovePreview preview,
                out string reason
            );

            Assert.That(created, Is.True, reason);
            Assert.That(preview.From, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(preview.To, Is.EqualTo(new Vector3(4f, 5f, 6f)));
            Assert.That(preview.BaseValue, Is.EqualTo(0.25f));
        }

        [Test]
        public void TryCreateRotate_RejectsNonFloatValue()
        {
            var rotateGo = new GameObject("preview-rotate");
            var targetGo = new GameObject("preview-rotate-target");
            try
            {
                var rotate = rotateGo.AddComponent<FDMiTransformRotate>();
                var data = rotateGo.AddComponent<FDMiBool>();
                var target = rotateGo.AddComponent<FDMiTransformRef>();
                target.data[0] = targetGo.transform;
                rotate.Value = data;
                rotate.target = target;
                rotate.outputCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(1f, 1f)
                );

                bool created = FDMiTransformPreview.TryCreateRotate(rotate, out _, out string reason);

                Assert.That(created, Is.False);
                StringAssert.Contains("FDMiFloat", reason);
            }
            finally
            {
                Object.DestroyImmediate(rotateGo);
                Object.DestroyImmediate(targetGo);
            }
        }
    }
}
