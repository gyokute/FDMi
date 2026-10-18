using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiEcefOrigin : FDMiBehaviour
    {
        [Tooltip("Geodetic latitude of the Unity-world origin in degrees. +90 at north pole.")]
        public float originLatitudeDeg = 35.681236f;
        [Tooltip("Geodetic longitude of the Unity-world origin in degrees east.")]
        public float originLongitudeDeg = 139.767125f;
        [Tooltip("Ellipsoidal height of the Unity-world origin in meters.")]
        public float originHeightM;
        [Tooltip("WGS84 semi-major axis in meters. Baked by Editor; do not hand-edit.")]
        public float semiMajorM = 6378137f;
        [Tooltip("WGS84 first eccentricity squared. Baked by Editor; do not hand-edit.")]
        public float eccentricitySq = 0.00669438f;
        [Tooltip("Baked ECEF position of the Unity-world origin in meters. xyz = XYZ.")]
        public Vector3 originEcefM;
        [Tooltip("Baked ENU basis at the origin: x=east, y=north, z=up in ECEF.")]
        public Vector3 eastEcef = Vector3.right;
        public Vector3 northEcef = Vector3.up;
        public Vector3 upEcef = Vector3.forward;

        bool baked;

        void Start()
        {
            if (!baked)
                Bake();
        }

        public void Bake()
        {
            float lat = originLatitudeDeg * Mathf.Deg2Rad;
            float lon = originLongitudeDeg * Mathf.Deg2Rad;
            float sinLat = Mathf.Sin(lat);
            float cosLat = Mathf.Cos(lat);
            float sinLon = Mathf.Sin(lon);
            float cosLon = Mathf.Cos(lon);
            float n = semiMajorM / Mathf.Sqrt(1f - eccentricitySq * sinLat * sinLat);
            originEcefM = new Vector3(
                (n + originHeightM) * cosLat * cosLon,
                (n * (1f - eccentricitySq) + originHeightM) * sinLat,
                (n + originHeightM) * cosLat * sinLon);
            eastEcef = new Vector3(-sinLon, 0f, cosLon);
            northEcef = new Vector3(-sinLat * cosLon, cosLat, -sinLat * sinLon);
            upEcef = new Vector3(cosLat * cosLon, sinLat, cosLat * sinLon);
            baked = true;
        }

        /// <summary>
        /// 原点からの ECEF オフセットで指定した位置での ENU 基底(ECEF→Unity)。
        /// チャンクは ECEF 直交系なのでチャンク番号は厳密だが、ENU 基底自体は緯度経度依存で回るため
        /// 遠距離でも成立するよう逆算は double で行う。チャンク通過時のみの呼出を想定。
        /// </summary>
        public Quaternion ENUAt(Vector3 offsetM)
        {
            if (!baked)
                Bake();
            double px = originEcefM.x + offsetM.x;
            double py = originEcefM.y + offsetM.y;
            double pz = originEcefM.z + offsetM.z;
            double p = System.Math.Sqrt(px * px + pz * pz);
            double b = semiMajorM * System.Math.Sqrt(1.0 - eccentricitySq);
            double theta = System.Math.Atan2(py * semiMajorM, p * b);
            double sinTheta = System.Math.Sin(theta);
            double cosTheta = System.Math.Cos(theta);
            // 測地緯度は Bowring 逆算。位置ベクトル素直の geocentric 緯度だと Bake の geodetic 基底と
            // 0.18 度(赤道付近で最大 0.19 度)ずれ、計器表示と描画が食い違う。
            // 分子は第二離心率 e'^2 * b(= a²/b − b)。e^2 * a では 0.0002 度残る。
            double lat = System.Math.Atan2(py + (semiMajorM * semiMajorM / b - b) * sinTheta * sinTheta * sinTheta,
                                            p - eccentricitySq * semiMajorM * cosTheta * cosTheta * cosTheta);
            double lon = System.Math.Atan2(pz, px);
            double sinLat = System.Math.Sin(lat);
            double cosLat = System.Math.Cos(lat);
            double sinLon = System.Math.Sin(lon);
            double cosLon = System.Math.Cos(lon);
            Vector3 east = new Vector3((float)-sinLon, 0f, (float)cosLon);
            Vector3 north = new Vector3((float)(-sinLat * cosLon), (float)cosLat, (float)(-sinLat * sinLon));
            Vector3 up = new Vector3((float)(cosLat * cosLon), (float)sinLat, (float)(cosLat * sinLon));
            // LookRotation(forward=north, upward=up) は列 (east, up, north) の Unity→ECEF 回転。
            // 必要なのは ECEF→Unity なので逆を返す。
            return Quaternion.Inverse(Quaternion.LookRotation(north, up));
        }
    }
}
