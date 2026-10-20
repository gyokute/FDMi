using FDMi.core.Editor.Inspector;
using FDMi.sync.Editor.Presentation;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor
{
    // 焼き付け済み分解値 (_position / _posMm / _chunkPosition) の読取表示と、
    // 現在 transform からの再焼き付けだけを行う (表示・Bake 以外の機能は足さない)。
    [CustomEditor(typeof(FDMiReferencePoint), true)]
    public class FDMiReferencePointEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var rp = (FDMiReferencePoint)target;
            Vector3 totalM = rp.getTotalPositionM();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Baked Reference Position (read-only)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector3Field("_position [m]", rp._position);
                EditorGUILayout.Vector3Field("_posMm -> [m]", FDMiReferencePoint.MmToM(rp._posMm));
                EditorGUILayout.Vector3Field("_chunkPosition -> [m]", rp._chunkPosition * FDMiReferencePoint.M_PER_CHUNK);
                EditorGUILayout.Vector3Field("Total [m]", totalM);
                EditorGUILayout.Vector3Field("Total [km]", totalM * 0.001f);
                EditorGUILayout.Vector3Field("Total [chunk]", totalM * FDMiReferencePoint.CHUNK_PER_M);
            }

            if (IsTransformDirty(rp))
                EditorGUILayout.HelpBox("Transform が焼き付け値と一致しません。「Bake from Transform」を押してください。(Bake 忘れ)", MessageType.Warning);

            if (GUILayout.Button("Bake from Transform"))
                BakeFromTransform(rp);
        }

        static bool IsTransformDirty(FDMiReferencePoint rp)
        {
            BakeCalc(rp.transform.position, new int[3], Vector3.zero,
                out Vector3 pos, out int[] mm, out Vector3 chunk);
            for (int i = 0; i < 3; i++)
                if (mm[i] != rp._posMm[i] || chunk[i] != rp._chunkPosition[i] || pos[i] != rp._position[i])
                    return true;
            return Quaternion.Angle(rp._rotation, rp.transform.rotation) > 0.001f;
        }

        void BakeFromTransform(FDMiReferencePoint rp)
        {
            // Why-not: Preview 中の transform は相対値 (小)。真値として焼くと破壊操作になるため Bake のみ禁じる (表示は変えない)。
            if (SphericalGroundPlacerWindow.IsPreviewingGlobal)
            {
                EditorUtility.DisplayDialog("PREVIEW中", "プレビュー値を焼けません。先に解除(Undo)してください。", "OK");
                return;
            }
            // Why-not: Udon 側 setPosition を呼ばず Editor 側 C# で計算する。
            // ランタイム Start() での取り込みを不要にし、AGENTS.md「予め計算できるものは Editor で」に従う。
            // 別途 FDMiRelativeGroundSync.Start に setPosition 復活が入っても、
            // Bake 済みなら Start は同値を再計算するだけで無害なため競合しない。
            BakeCalc(rp.transform.position, new int[3], Vector3.zero,
                out Vector3 pos, out int[] mm, out Vector3 chunk);

            Undo.RecordObject(rp, "Bake FDMiReferencePoint from Transform");
            serializedObject.Update();
            serializedObject.FindProperty("_position").vector3Value = pos;
            SerializedProperty mmProp = serializedObject.FindProperty("_posMm");
            for (int i = 0; i < 3; i++)
                mmProp.GetArrayElementAtIndex(i).intValue = mm[i];
            serializedObject.FindProperty("_chunkPosition").vector3Value = chunk;
            serializedObject.FindProperty("_rotation").quaternionValue = rp.transform.rotation;
            serializedObject.ApplyModifiedProperties();
        }

        // FDMiReferencePoint.setPosition と同一の剥離計算 (500 m 閾値・1 km 丸め・1 chunk 上限の反復)。
        // float / Mathf の同一式なので IEEE754 単精度演算が一致し、Bake 結果はランタイム再計算と同一になる。
        // Why-not: private にせず internal static に開く。球面プレーサ (PlaceGroundsUseCase) が
        // 同一式を再実装すると drift の温床になるため、唯一の真実として共有する。
        internal static void BakeCalc(Vector3 pos, int[] srcMm, Vector3 srcChunk,
            out Vector3 position, out int[] posMm, out Vector3 chunkPosition)
        {
            Vector3 v = pos, k = srcChunk;
            int[] m = (int[])srcMm.Clone();
            bool updateChunkPos = false;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(v[i]) <= FDMiReferencePoint.CARRY_M) continue;
                updateChunkPos = true;
                while (Mathf.Abs(v[i]) > FDMiReferencePoint.CARRY_M)
                {
                    int s = (int)(Mathf.Abs(v[i]) * 0.001f + 0.5f);
                    if (s < 1) s = 1;
                    if (s > FDMiReferencePoint.CARRY_KM_MAX) s = FDMiReferencePoint.CARRY_KM_MAX;
                    int d = v[i] < 0f ? -s : s;
                    m[i] += d * 1000000;
                    v[i] -= d * 1000f;
                    if (m[i] >= FDMiReferencePoint.MM_PER_CHUNK) { k[i] += 1f; m[i] -= FDMiReferencePoint.MM_PER_CHUNK; }
                    else if (m[i] <= -FDMiReferencePoint.MM_PER_CHUNK) { k[i] -= 1f; m[i] += FDMiReferencePoint.MM_PER_CHUNK; }
                }
            }
            if (updateChunkPos) chunkPosition = k;
            else chunkPosition = srcChunk;
            position = v;
            posMm = m;
        }
    }
}
