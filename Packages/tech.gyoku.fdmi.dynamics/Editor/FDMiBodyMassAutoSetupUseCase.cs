using System.Collections.Generic;
using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // FDMiBodyMass の配線を機体 (名前空間ルート) から解決し、質量特性を焼き付ける。
    //   body : 機体の Rigidbody (階層パス順の先頭)
    // 質量・重心の FDMiFloat は core の ResolveDataPathsUseCase がパスから解決する。
    // 焼き付けは Inspector を開いたときと、designMass などを変更したとき (FDMiBehaviourEditor の
    // 変更検出) に走る。値が変わらないときは scene を汚さない (FDMiBodyMassBake の Assign)。
    public class FDMiBodyMassAutoSetupUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            FDMiBodyMass mass = target as FDMiBodyMass;
            if (mass == null)
                return;

            List<Rigidbody> bodies = FDMiAircraftScope.FindAll<Rigidbody>(mass);
            if (bodies.Count > 0)
            {
                SerializedObject so = new SerializedObject(mass);
                so.FindProperty(nameof(FDMiBodyMass.body)).objectReferenceValue = bodies[0];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // MAC重心率は ~/Physics/CG の焼き付け時値を読む。データパスの解決と同じタイミングで
            // 必ず参照が入っている状態にするため、ここで明示的に解決してから焼く。
            new ResolveDataPathsUseCase().Execute(mass);

            FDMiBodyMassBake.Bake(mass);
        }

        public void ExecuteAll()
        {
            FDMiBodyMass[] masses = UnityEngine.Object.FindObjectsByType<FDMiBodyMass>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < masses.Length; i++)
                Execute(masses[i]);
        }
    }
}
