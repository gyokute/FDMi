using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Application.UseCases
{
    // プレビュー中心 -> transform のみ書換え。Bake値・syncManager/parentRefPoint/rootRefPoint/index に触らない。
    // Why-not: PreviewShiftScope (IDisposable) を作らない。退避スタックの二重管理はバグ源。
    // 復元は Undo が担い、解除は Undo にする (原点再適用は絶対/相対を取り違えるため使わない)。
    // Why-not: SetParent はしない。Resolve配線の ParentIndex 検証を汚すため。親は動かさない。
    public sealed class PreviewShiftUseCase
    {
        // 地面のみ (is FDMiRelativeGroundSync) か sync 全件か。既定は全件。
        public int Apply(double previewLatDeg, double previewLonDeg, float previewAltM, bool groundOnly)
        {
            EarthMath.LatLonAltToEcef(previewLatDeg, previewLonDeg, previewAltM,
                out Vector3 previewEcef, out Quaternion previewRot);

            FDMiReferencePoint[] all = Object.FindObjectsByType<FDMiReferencePoint>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Preview Shift Grounds");
            int shifted = 0;
            for (int i = 0; i < all.Length; i++)
            {
                FDMiReferencePoint rp = all[i];
                // Why-not: Manager の Bake total は 0 (ECEF ではない)。ViewFromEcef(0) は数千km飛ぶため除外。PlayerPosition は ECEF 系なので残す。
                if (rp is FDMiRelativeObjectSyncManager) continue;
                if (groundOnly && !(rp is FDMiRelativeGroundSync))
                    continue;
                Vector3 tileTotal = EarthMath.TotalM(rp._position, rp._posMm, rp._chunkPosition);
                EarthMath.ViewFromEcef(tileTotal, rp._rotation, previewEcef, previewRot,
                    out Vector3 viewPos, out Quaternion viewRot);
                Undo.RecordObject(rp.transform, "Preview Shift Grounds");
                rp.transform.SetPositionAndRotation(viewPos, viewRot);
                shifted++;
            }
            Undo.CollapseUndoOperations(group);
            return shifted;
        }
    }
}
