using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Inspector
{
    // E-07 Snap 補助。transform.rotation = q_ref⁻¹ ⊗ q_basis(lat, lon) ⊗ R_y(yaw) を 1 ボタンで適用する。
    // 緯度経度高度 yaw は静的アンカーだけが持つため、動的アンカーにはボタンを出さない (§9.2.2)。
    // Why-not: 焼きの基準は initialAnchor の絶対姿勢である (§3.6)。別の基準アンカーを作らない。
    // Why-not: 基底を `Editor` と略さない。名前空間 `FDMi.sync.Editor` が型名を隠す (CS0118)。
    [CustomEditor(typeof(FDMiSyncAnchor), true)]
    public class FDMiSyncAnchorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FDMiStaticObjectSync anchor = target as FDMiStaticObjectSync;
            if (anchor == null) return;

            EditorGUILayout.Space();
            if (!GUILayout.Button("Snap to lat/lon (E-07)")) return;

            FDMiSyncManager manager = Object.FindObjectOfType<FDMiSyncManager>();
            if (manager == null || manager.initialAnchor == null)
            {
                Debug.LogError("[FDMiSync] V-11: FDMiSyncManager と initialAnchor が必要です。");
                return;
            }

            long[] p = new long[3];
            FDMiEarth.LatLonAltToAbsMm(anchor.latitude, anchor.longitude, anchor.height, p);
            Quaternion qAbs = FDMiEarth.BasisWithYaw(anchor.latitude, anchor.longitude, anchor.yaw);
            Quaternion qRef = FDMiSyncMath.UnpackQuat(FDMiSyncAnchorFields.Rot(manager.initialAnchor));

            Transform t = (anchor.body != null) ? anchor.body.transform : anchor.transform;
            Undo.RecordObject(t, "FDMi Sync Snap");
            t.SetPositionAndRotation(
                FDMiEarth.AbsMmToView(FDMiSyncAnchorFields.AbsMm(manager.initialAnchor), qRef, p),
                Quaternion.Inverse(qRef) * qAbs);
        }
    }
}
