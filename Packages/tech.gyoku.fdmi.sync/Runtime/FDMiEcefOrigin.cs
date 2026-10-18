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

        public Vector3 UnityToEcefOffsetM(Vector3 unityM)
        {
            if (!baked)
                Bake();
            return eastEcef * unityM.x + northEcef * unityM.z + upEcef * unityM.y;
        }

        public Vector3 EcefOffsetToUnityM(Vector3 offsetM)
        {
            if (!baked)
                Bake();
            return new Vector3(Vector3.Dot(offsetM, eastEcef), Vector3.Dot(offsetM, upEcef), Vector3.Dot(offsetM, northEcef));
        }

        public float GeodeticHeightM(Vector3 unityM)
        {
            if (!baked)
                Bake();
            return Vector3.Dot(UnityToEcefOffsetM(unityM), upEcef);
        }
    }
}
