using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor
{
    [CustomEditor(typeof(FDMiEcefOrigin))]
    public class FDMiEcefOriginEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            FDMiEcefOrigin origin = target as FDMiEcefOrigin;
            if (!origin)
                return;
            if (GUILayout.Button("Bake Origin"))
            {
                origin.Bake();
                EditorUtility.SetDirty(origin);
            }
            EditorGUILayout.HelpBox("Unity +X=east, +Y=up, +Z=north at the origin. Bake writes originEcefM and the ENU basis.", MessageType.Info);
        }
    }
}
