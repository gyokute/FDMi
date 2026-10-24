using System;
using UnityEngine;
using VRC.SDK3.Components;

namespace FDMi.sync.Editor.Domain
{
    // §3.1 / §3.4 / E-03 / E-04 の Editor 側の唯一の実装。double で計算する。
    // Why-not: Runtime では double を使わない (§1.4)。緯度経度から絶対 mm を焼くのは Editor だけで、
    // 6.371e9 mm を float にすると ulp が 512 mm になり焼けない (§4.2)。
    public static class FDMiEarth
    {
        public const double R_EARTH_M = 6371000.0;
        public const double R_EARTH_MM = 6371000000.0;
        const double Deg2Rad = Math.PI / 180.0;
        const double Rad2Deg = 180.0 / Math.PI;

        // §3.1: 緯度経度高度 → FEF mm。符号・日付変更線・極に分岐はない (cos/sin が吸収する)。
        public static void LatLonAltToAbsMm(double latDeg, double lonDeg, double altM, long[] dst)
        {
            double r = R_EARTH_M + altM;
            double latR = latDeg * Deg2Rad;
            double lonR = lonDeg * Deg2Rad;
            dst[0] = RoundHalfAwayFromZero(r * Math.Cos(latR) * Math.Cos(lonR) * 1000.0);
            dst[1] = RoundHalfAwayFromZero(r * Math.Sin(latR) * 1000.0);
            dst[2] = RoundHalfAwayFromZero(r * Math.Cos(latR) * Math.Sin(lonR) * 1000.0);
        }

        // §3.1 の逆変換。h は r − R の桁落ちを避けるため (s − R²) / (r + R) で求める。
        public static void AbsMmToLatLonAlt(long[] absMm, out double latDeg, out double lonDeg, out double altM)
        {
            double sx = absMm[0];
            double sy = absMm[1];
            double sz = absMm[2];
            double s = sx * sx + sy * sy + sz * sz;
            double r = Math.Sqrt(s);
            latDeg = Math.Asin(sy / r) * Rad2Deg;
            lonDeg = Math.Atan2(sz, sx) * Rad2Deg;
            // 極では lon を 0 に固定し、+180 は −180 に正規化する (§3.1)。
            if (latDeg >= 90.0 - 1e-9 || latDeg <= -90.0 + 1e-9) lonDeg = 0.0;
            else if (lonDeg >= 180.0) lonDeg -= 360.0;
            altM = (s - R_EARTH_MM * R_EARTH_MM) / (r + R_EARTH_MM) * 0.001;
        }

        // §3.4 の局所基底 (FEF 成分)。
        public static Vector3 East(double latDeg, double lonDeg)
        {
            double lonR = lonDeg * Deg2Rad;
            return new Vector3((float)(-Math.Sin(lonR)), 0f, (float)Math.Cos(lonR));
        }

        public static Vector3 Up(double latDeg, double lonDeg)
        {
            double latR = latDeg * Deg2Rad;
            double lonR = lonDeg * Deg2Rad;
            return new Vector3(
                (float)(Math.Cos(latR) * Math.Cos(lonR)),
                (float)Math.Sin(latR),
                (float)(Math.Cos(latR) * Math.Sin(lonR)));
        }

        public static Vector3 North(double latDeg, double lonDeg)
        {
            double latR = latDeg * Deg2Rad;
            double lonR = lonDeg * Deg2Rad;
            return new Vector3(
                (float)(-Math.Sin(latR) * Math.Cos(lonR)),
                (float)Math.Cos(latR),
                (float)(-Math.Sin(latR) * Math.Sin(lonR)));
        }

        // §3.4。q_basis は結果規定 (q·x̂ = E、q·ŷ = U、q·ẑ = N) であり閉形式を規定しない。
        // LookRotation(N, U) は z 軸を N、y 軸を U にし、x 軸を U×N = E にする。
        public static Quaternion Basis(double latDeg, double lonDeg)
        {
            return Quaternion.LookRotation(North(latDeg, lonDeg), Up(latDeg, lonDeg));
        }

        // 接平面内のモデルの向き (§3.4)。q_abs = q_basis ⊗ R_y(yaw)。
        public static Quaternion BasisWithYaw(double latDeg, double lonDeg, double yawDeg)
        {
            return Basis(latDeg, lonDeg) * Quaternion.AngleAxis((float)yawDeg, Vector3.up);
        }

        // §4.2 と同じ規則を double で実装する。float 版 (FDMiSyncMath) と規則を変えない。
        public static long RoundHalfAwayFromZero(double x)
        {
            long t = (long)x;
            double f = x - t;
            if (x >= 0.0) { if (f >= 0.5) t += 1L; }
            else { if (f <= -0.5) t -= 1L; }
            return t;
        }

        // §3.6。シーン (view) → FEF。origin と qOrigin は initialAnchor の絶対姿勢。
        public static void ViewToAbsMm(long[] origin, Quaternion qOrigin, Vector3 viewM, long[] dst)
        {
            Vector3 p = qOrigin * (viewM * 1000f);
            dst[0] = origin[0] + RoundHalfAwayFromZero(p.x);
            dst[1] = origin[1] + RoundHalfAwayFromZero(p.y);
            dst[2] = origin[2] + RoundHalfAwayFromZero(p.z);
        }

        // §7.1。FEF → シーン (view)。差を long で先に取る。
        public static Vector3 AbsMmToView(long[] origin, Quaternion qOrigin, long[] absMm)
        {
            Vector3 d = new Vector3(
                (float)(absMm[0] - origin[0]) * 0.001f,
                (float)(absMm[1] - origin[1]) * 0.001f,
                (float)(absMm[2] - origin[2]) * 0.001f);
            return Quaternion.Inverse(qOrigin) * d;
        }

        // E-03 / E-04。車両 (body、無ければ view) から stationEnterPlayerLocation への局所姿勢。
        // 焼き (E-04) と検証 (E-03) の両方がこの 1 関数を呼ぶ。
        // Why-not: 同じ式を焼きと検証に 2 度書かない。片方だけ直されると検証が常に通る嘘になる (§11.1)。
        public static void SeatRel(FDMiStationAnchor s, int[] dstMm, out Quaternion relRot)
        {
            VRCStation station = s.GetComponent<VRCStation>();
            Transform vehicle = (s.vehicleAnchor.body != null) ? s.vehicleAnchor.body.transform
                : (s.vehicleAnchor.view != null ? s.vehicleAnchor.view : s.vehicleAnchor.transform);
            Transform seat = (station.stationEnterPlayerLocation != null)
                ? station.stationEnterPlayerLocation
                : station.transform;

            Vector3 p = vehicle.InverseTransformPoint(seat.position);
            dstMm[0] = (int)RoundHalfAwayFromZero(p.x * 1000.0);
            dstMm[1] = (int)RoundHalfAwayFromZero(p.y * 1000.0);
            dstMm[2] = (int)RoundHalfAwayFromZero(p.z * 1000.0);
            relRot = Quaternion.Inverse(vehicle.rotation) * seat.rotation;
        }
    }
}
