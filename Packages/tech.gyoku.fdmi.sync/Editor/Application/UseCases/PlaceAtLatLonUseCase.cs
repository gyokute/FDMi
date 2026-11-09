using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Application.UseCases
{
    // 緯度経度高度からシーン姿勢を作る共通ユースケース。
    // Why-not: meshmapaids と CNS のインポータで FDMiEarth を直接呼ばない。
    // 焼き (BakeSyncSceneUseCase) と同じ精度 (PackQuat) で基準姿勢を作り、
    // 生成側だけ full 精度になって 1e-5 rad ずれるのを防ぐ。
    public static class PlaceAtLatLonUseCase
    {
        // 静的アンカーの緯度経度高度 yaw から、焼きと同じ絶対姿勢を作る。
        public static void StaticPose(FDMiStaticObjectSync anchor, long[] absMm, out Quaternion rot)
        {
            FDMiEarth.LatLonAltToAbsMm(anchor.latitude, anchor.longitude, anchor.height, absMm);
            var packed = new short[4];
            FDMiSyncMath.PackQuat(
                FDMiEarth.BasisWithYaw(anchor.latitude, anchor.longitude, anchor.yaw),
                packed
            );
            rot = FDMiSyncMath.UnpackQuat(packed);
        }

        public static void FacilityPose(double latDeg, double lonDeg, double heightM, long[] absMm)
        {
            FDMiEarth.LatLonAltToAbsMm(latDeg, lonDeg, heightM, absMm);
        }

        // 基準アンカーの frame (シーン / view) への変換。
        public static Vector3 ToLocal(long[] refAbsMm, Quaternion refRot, long[] absMm)
        {
            return FDMiEarth.AbsMmToView(refAbsMm, refRot, absMm);
        }

        // 選択オブジェクトを緯度経度へ置く。親の Transform は Unity が辿る。
        public static void PlaceTransform(
            Transform target,
            long[] refAbsMm,
            Quaternion refRot,
            double latDeg,
            double lonDeg,
            double heightM,
            float yawDeg
        )
        {
            var absMm = new long[3];
            FacilityPose(latDeg, lonDeg, heightM, absMm);
            target.position = ToLocal(refAbsMm, refRot, absMm);
            target.rotation = Quaternion.Inverse(refRot) * FDMiEarth.BasisWithYaw(latDeg, lonDeg, yawDeg);
        }

        // 緯度経度に静的アンカーを 1 個作る。
        public static FDMiStaticObjectSync CreateStaticAnchor(
            string name,
            Transform parent,
            FDMiSyncManager manager,
            double latDeg,
            double lonDeg,
            double heightM,
            float yawDeg,
            long[] refAbsMm,
            Quaternion refRot
        )
        {
            var go = EditorUtility.CreateGameObjectWithHideFlags(
                name,
                HideFlags.None,
                typeof(FDMiStaticObjectSync)
            );
            if (parent != null) go.transform.SetParent(parent, false);

            var anchor = go.GetComponent<FDMiStaticObjectSync>();
            anchor.manager = manager;
            anchor.latitude = (float)latDeg;
            anchor.longitude = (float)lonDeg;
            anchor.height = (float)heightM;
            anchor.yaw = yawDeg;

            var absMm = new long[3];
            StaticPose(anchor, absMm, out var rot);
            go.transform.SetPositionAndRotation(
                ToLocal(refAbsMm, refRot, absMm),
                Quaternion.Inverse(refRot) * rot
            );
            return anchor;
        }
    }
}
