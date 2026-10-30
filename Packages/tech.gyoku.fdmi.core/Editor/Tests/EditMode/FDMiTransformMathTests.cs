using FDMi.core;
using NUnit.Framework;
using UnityEngine;

namespace FDMi.core.Editor.Tests
{
    public class FDMiTransformMathTests
    {
        [Test]
        public void EvaluatePosition_LinearCurve_ReturnsLerp()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

            Vector3 result = FDMiTransformMath.EvaluatePosition(
                Vector3.zero,
                new Vector3(1f, 2f, 3f),
                curve,
                0.5f
            );

            Assert.That(
                Vector3.Distance(result, new Vector3(0.5f, 1f, 1.5f)),
                Is.LessThan(1e-5f)
            );
        }

        [Test]
        public void EvaluateEuler_ScalesAxisByCurve()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 10f));

            Vector3 result = FDMiTransformMath.EvaluateEuler(Vector3.forward, curve, 0.5f);

            Assert.That(Vector3.Distance(result, new Vector3(0f, 0f, 5f)), Is.LessThan(1e-5f));
        }
    }
}
