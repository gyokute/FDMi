using System;
using UnityEngine;

namespace FDMi.sync.Editor.Domain
{
    // 経緯度<->Unity系ECEF/ENU の純粋関数群。double で計算し、出力のみ float。
    // Why-not: 変換式の唯一の真実は FDMiReferencePoint.EARTH_RADIUS_M (=6371000f)。
    // WGS84楕円体・ジオイド・磁気偏角は作らない (YAGNI)。球+R+真北で足りる。
    public static class EarthMath
    {
        const double Deg2Rad = Math.PI / 180.0;
        const double Rad2Deg = 180.0 / Math.PI;

        // 入力: latDeg,lonDeg[deg], altM[m]。SIのみ。
        // Unity系ECEF (標準ECEFのZを反転)。反転しないとUnity回転で表せない鏡像になるため。
        public static void LatLonAltToEcef(double latDeg, double lonDeg, double altM,
            out Vector3 ecef, out Quaternion rotation)
        {
            double phi = latDeg * Deg2Rad;
            double lam = lonDeg * Deg2Rad;
            double r = (double)FDMiReferencePoint.EARTH_RADIUS_M + altM;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);
            double cosLam = Math.Cos(lam);
            double sinLam = Math.Sin(lam);

            double ex = r * cosPhi * cosLam;
            double ey = r * sinPhi;
            double ez = -r * cosPhi * sinLam;
            ecef = new Vector3((float)ex, (float)ey, (float)ez);

            Vector3 up = ecef.normalized;
            Vector3 north = new Vector3(
                (float)(-sinPhi * cosLam),
                (float)Math.Cos(phi),
                (float)(sinPhi * sinLam)).normalized;
            // Why-not: この rotation のローカルxは West になる。E,U,N を (x=E,y=U,z=N) に
            // 並べると行列式-1 の鏡像で回転にできないため。東西ステップは east ベクトルで計算し、
            // UI は East+/West- で見せる。LookRotation(north, up) / forward=North, up=Up に一本化する。
            rotation = Quaternion.LookRotation(north, up);
        }

        public static Vector3 LatLonAltToEcef(double latDeg, double lonDeg, double altM)
        {
            LatLonAltToEcef(latDeg, lonDeg, altM, out Vector3 ecef, out Quaternion _);
            return ecef;
        }

        public static Quaternion LatLonToRotation(double latDeg, double lonDeg)
        {
            LatLonAltToEcef(latDeg, lonDeg, 0.0, out Vector3 _, out Quaternion rotation);
            return rotation;
        }

        // Bake済み total (ECEF) から緯度経度高度を逆算。吸着・ミニマップ表示用。
        public static void EcefToLatLon(Vector3 ecef, out double latDeg, out double lonDeg, out double altM)
        {
            double ex = ecef.x;
            double ey = ecef.y;
            double ez = ecef.z;
            double r = Math.Sqrt(ex * ex + ey * ey + ez * ez);
            latDeg = Math.Asin(ey / r) * Rad2Deg;
            lonDeg = Math.Atan2(-ez, ex) * Rad2Deg;
            altM = r - (double)FDMiReferencePoint.EARTH_RADIUS_M;
        }

        // Bake済み3層値から total [m] を Editor 側で再計算 (Udon呼出し不要)。
        public static Vector3 TotalM(Vector3 position, int[] posMm, Vector3 chunkPosition)
        {
            return position + FDMiReferencePoint.MmToM(posMm) + chunkPosition * FDMiReferencePoint.M_PER_CHUNK;
        }

        // タイル間相対 (プレビュー/ランタイム共通、viewForRoot と同形。Editorでは localCell=0)。
        public static void ViewFromEcef(Vector3 ecefTile, Quaternion rotTile,
            Vector3 ecefPreview, Quaternion rotPreview,
            out Vector3 viewPos, out Quaternion viewRot)
        {
            Quaternion inv = Quaternion.Inverse(rotPreview);
            viewPos = inv * (ecefTile - ecefPreview);
            viewRot = (inv * rotTile).normalized;
        }

        // グリッド刻み (メートル正方形。deg正方形にしない。Why-not: 高緯度で横に潰れる)。
        public static double DeltaLatDeg(double sizeM, double altM)
        {
            double r = (double)FDMiReferencePoint.EARTH_RADIUS_M + altM;
            return sizeM / r * Rad2Deg;
        }

        public static double DeltaLonDeg(double sizeM, double latDeg, double altM)
        {
            double r = (double)FDMiReferencePoint.EARTH_RADIUS_M + altM;
            return sizeM / (r * Math.Cos(latDeg * Deg2Rad)) * Rad2Deg;
        }
    }
}
