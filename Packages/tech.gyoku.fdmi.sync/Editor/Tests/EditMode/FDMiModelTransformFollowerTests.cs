using FDMi.core;
using NUnit.Framework;
using UnityEngine;

namespace FDMi.sync.Editor.Tests
{
    // §8.2 M-20。root の間、機体モデルは Manager が原点に写す Unity world 軸に一致する。
    // 位置だけ 0 にすると parent 代入が保存する世界回転が残り、機体モデル (と station) が
    // 同期姿勢の root フレームから回転してずれる。
    public class FDMiModelTransformFollowerTests
    {
        [Test]
        public void M20_root切替で機体モデルはrootフレームの姿勢になる()
        {
            GameObject modelRootGo = new GameObject("modelRoot");
            GameObject bodyGo = new GameObject("body");
            GameObject modelGo = new GameObject("modelObjects");
            GameObject flagGo = new GameObject("isRoot");
            try
            {
                bodyGo.transform.SetParent(modelRootGo.transform, true);
                modelGo.transform.SetParent(bodyGo.transform, true);

                FDMiBool isRoot = flagGo.AddComponent<FDMiBool>();
                FDMiModelTransformFollower follower = modelGo.AddComponent<FDMiModelTransformFollower>();
                follower.IsRoot = isRoot;
                follower.parentWhenTrue = modelRootGo.transform;
                follower.parentWhenFalse = bodyGo.transform;

                // 自由移動中: モデルは body の世界姿勢に従う。
                bodyGo.transform.SetPositionAndRotation(
                    new Vector3(120f, 0f, -30f), Quaternion.Euler(0f, 40f, 0f));
                isRoot.data[0] = false;
                follower.OnChange();
                AssertVector(bodyGo.transform.position, modelGo.transform.position, "非 root の位置");
                AssertRotation(bodyGo.transform.rotation, modelGo.transform.rotation, "非 root の姿勢");

                // root 切替: モデルを世界原点・identity へ戻す。body の原点への移動は Manager が行う。
                isRoot.data[0] = true;
                follower.OnChange();
                AssertVector(Vector3.zero, modelGo.transform.position, "root の位置");
                AssertRotation(Quaternion.identity, modelGo.transform.rotation, "root の姿勢");

                // 機体が向きを変えた後の再搭乗。切替時の世界回転を持ち越さない。
                bodyGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 130f, 0f));
                isRoot.data[0] = false;
                follower.OnChange();
                AssertRotation(bodyGo.transform.rotation, modelGo.transform.rotation, "再搭乗後の非 root の姿勢");
                isRoot.data[0] = true;
                follower.OnChange();
                AssertRotation(Quaternion.identity, modelGo.transform.rotation, "再搭乗後の root の姿勢");
            }
            finally
            {
                Object.DestroyImmediate(modelGo);
                Object.DestroyImmediate(bodyGo);
                Object.DestroyImmediate(modelRootGo);
                Object.DestroyImmediate(flagGo);
            }
        }

        static void AssertVector(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-5, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 1e-5, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 1e-5, label + ".z");
        }

        static void AssertRotation(Quaternion expected, Quaternion actual, string label)
        {
            Assert.IsTrue(Quaternion.Angle(expected, actual) < 1e-3f, label);
        }
    }
}
