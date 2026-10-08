using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor
{
    [CustomEditor(typeof(FDMiEcefSync))]
    public class FDMiEcefSyncEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            FDMiEcefSync sync = target as FDMiEcefSync;
            if (!sync)
                return;
            float rangeKm = 21474.83647f * sync.cellMeters / 1000f;
            EditorGUILayout.HelpBox($"Range per axis: about {rangeKm:F1} km at {sync.cellMeters * 100f:F1} cm steps. One behaviour carries 40 bytes.", MessageType.Info);
        }
    }
}
