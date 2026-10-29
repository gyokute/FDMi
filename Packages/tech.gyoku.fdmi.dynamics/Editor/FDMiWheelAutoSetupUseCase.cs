using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // FDMiWheelCollider の実行時前提を自動で整える。ホイールを選んだ時と値を変えた時に走る。
    //   - めり込みの下限コライダー: 最大圧縮位置のタイヤと同径の SphereCollider をホイール自身に付ける
    //     (中心 = transform.position、半径 = tireRadius)。サスが底付きする位置がそのまま球の接地面になり、
    //     高速接地で機体が滑走路を突き抜けるのを物理的に止める。タイヤ力の計算には関与しない。
    //     Layer は人間が決めるため触らない。既にあれば半径を合わせるだけで、何度走らせても同じ結果になる。
    //   - 機体 Rigidbody の連続衝突判定: 物理側でも突き抜けを止める。
    //   - 係数の焼き付け: FDMiWheelBake。設計値・body.mass を変えたら選び直すだけで追従する。
    public class FDMiWheelAutoSetupUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            FDMiWheelCollider wheel = target as FDMiWheelCollider;
            if (wheel == null)
                return;

            SphereCollider sphere = wheel.GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = wheel.gameObject.AddComponent<SphereCollider>();
            sphere.center = Vector3.zero;
            sphere.radius = wheel.tireRadius;

            if (wheel.body != null)
                wheel.body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (FDMiWheelBake.Bake(wheel))
                EditorUtility.SetDirty(wheel);
        }

        public void ExecuteAll()
        {
            FDMiWheelCollider[] wheels = UnityEngine.Object.FindObjectsByType<FDMiWheelCollider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            for (int i = 0; i < wheels.Length; i++)
                Execute(wheels[i]);
        }
    }
}
