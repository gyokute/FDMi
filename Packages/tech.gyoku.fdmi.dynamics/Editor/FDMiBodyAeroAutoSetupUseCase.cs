using System.Collections.Generic;
using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 胴体の body を機体 (名前空間ルート) から自動で解決する。翼と同じ規則。
    // 胴体が複数ある機体は考えない (先頭を採る)。手で入れ替えたい場合は配線し直す。
    public class FDMiBodyAeroAutoSetupUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            FDMiBodyAero bodyAero = target as FDMiBodyAero;
            if (bodyAero == null)
                return;

            List<Rigidbody> bodies = FDMiAircraftScope.FindAll<Rigidbody>(bodyAero);
            if (bodies.Count == 0)
                return;

            SerializedObject so = new SerializedObject(bodyAero);
            so.FindProperty(nameof(FDMiBodyAero.body)).objectReferenceValue = bodies[0];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void ExecuteAll()
        {
            FDMiBodyAero[] bodyAeros = UnityEngine.Object.FindObjectsByType<FDMiBodyAero>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bodyAeros.Length; i++)
                Execute(bodyAeros[i]);
        }
    }
}
