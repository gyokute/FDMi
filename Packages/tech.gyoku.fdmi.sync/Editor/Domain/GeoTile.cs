using UnityEngine;

namespace FDMi.sync.Editor.Domain
{
    // 緯度経度タイルの値束。シリアライズしない (Bake値と命名に残し再現可能にする)。
    // Why-not: Runtime に緯度経度を持たせない。要るのは焼付け済みECEFだけであり、
    // 緯度経度は Editor Entity と命名 (Ground_E{lon}_N{lat}) に残す。Bake思想に合致。
    public sealed class GeoTile
    {
        public double LatDeg;
        public double LonDeg;
        public float AltM;
        public Vector3 Ecef;
        public Quaternion Rotation;
        public string Name;

        public GeoTile(double latDeg, double lonDeg, float altM)
        {
            LatDeg = latDeg;
            LonDeg = lonDeg;
            AltM = altM;
            EarthMath.LatLonAltToEcef(latDeg, lonDeg, altM, out Ecef, out Rotation);
            Name = PlaceName(latDeg, lonDeg);
        }

        public static string PlaceName(double latDeg, double lonDeg)
        {
            return string.Format("Ground_E{0:F3}_N{1:F3}", lonDeg, latDeg);
        }
    }
}
