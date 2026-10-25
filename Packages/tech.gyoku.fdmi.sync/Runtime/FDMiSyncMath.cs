using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §9.6。状態を持たない static 数学ヘルパー。GameObject に付けない (V-10)。
    // double を使わない。Mathf.Sqrt は float 版だけを使う (U-01)。
    // Why-not: plain な static class を作らない (§14.2)。Udon は UdonSharpBehaviour 派生の型しか
    // プログラムとして登録できないため、static メソッドはこの型に置く。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiSyncMath : UdonSharpBehaviour
    {
        public const int Q_SCALE = 32767;

        // §4.2。round half away from zero の唯一の実装。
        // Why-not: Mathf.Round は banker's rounding なので tie で期待値とずれる。(long)x 単独では tie が落ちる。
        public static long RoundHalfAwayFromZero(float x)
        {
            long t = (long)x;
            float f = x - (float)t;
            if (x >= 0f) { if (f >= 0.5f) t += 1L; }
            else { if (f <= -0.5f) t -= 1L; }
            return t;
        }

        // a ⊗ b。Why-not: 成分を自前で書かない。Unity の Quaternion 積が同じ演算で、ステップ数が少ない。
        public static Quaternion QMul(Quaternion a, Quaternion b)
        {
            return a * b;
        }

        public static Quaternion QInv(Quaternion q)
        {
            return new Quaternion(-q.x, -q.y, -q.z, q.w);
        }

        public static Quaternion QNormalize(Quaternion q)
        {
            float n = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (n < 1e-6f) return Quaternion.identity;
            float inv = 1f / n;
            return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
        }

        // Rotate(q, v) = q·v·q⁻¹。
        public static Vector3 QRotate(Quaternion q, Vector3 v)
        {
            return q * v;
        }

        // §4.5 の符号化。dst は長さ 4 の事前確保配列。
        public static void PackQuat(Quaternion q, short[] dst)
        {
            float x = q.x;
            float y = q.y;
            float z = q.z;
            float w = q.w;
            float n = Mathf.Sqrt(x * x + y * y + z * z + w * w);
            if (n < 1e-6f) { dst[0] = 0; dst[1] = 0; dst[2] = 0; dst[3] = (short)Q_SCALE; return; }
            float inv = 1f / n;
            x *= inv; y *= inv; z *= inv; w *= inv;

            // 符号の代表元: 最大成分を必ず正にする (同値なら最小の index)。
            // Why-not: w を基準にしない。180° 回転で w = 0 になり符号の基準が消える。
            int k = 0;
            float m = Mathf.Abs(x);
            float a1 = Mathf.Abs(y); if (a1 > m) { m = a1; k = 1; }
            float a2 = Mathf.Abs(z); if (a2 > m) { m = a2; k = 2; }
            float a3 = Mathf.Abs(w); if (a3 > m) { m = a3; k = 3; }
            float sign = w;
            if (k == 0) sign = x;
            else if (k == 1) sign = y;
            else if (k == 2) sign = z;
            if (sign < 0f) { x = -x; y = -y; z = -z; w = -w; }

            dst[0] = PackComponent(x);
            dst[1] = PackComponent(y);
            dst[2] = PackComponent(z);
            dst[3] = PackComponent(w);
        }

        // Why-not: clamp を省かない。正規化の float 誤差で |u| が 1 をわずかに超えると 32768 の丸めが出て、
        // short の範囲 (−32768..32767) の負側代表 −32768 が現れる。|c| ≤ 32767 を保証する。
        static short PackComponent(float u)
        {
            return (short)ClampL(RoundHalfAwayFromZero(u * Q_SCALE), -Q_SCALE, Q_SCALE);
        }

        // §4.5 の復元。手順 4 の正規化は必須 (clamp が働くと |v| が 1 からずれる)。
        public static Quaternion UnpackQuat(short[] src)
        {
            // Why-not: `src[i] / Q_SCALE` と書かない。Q_SCALE は int なので整数除算になり、
            // |c| < Q_SCALE の成分がすべて 0 に落ちる (§4.5 復元手順 1 は (float)c_i / Q_SCALE)。
            float x = (float)src[0] / Q_SCALE;
            float y = (float)src[1] / Q_SCALE;
            float z = (float)src[2] / Q_SCALE;
            float w = (float)src[3] / Q_SCALE;
            float s = x * x + y * y + z * z + w * w;
            // 正常な符号化出力の s は 1 ± 6.2e-5。この分岐は零・破損だけを identity に落とす。
            if (s < 0.5f) return Quaternion.identity;
            float inv = 1f / Mathf.Sqrt(s);
            return new Quaternion(x * inv, y * inv, z * inv, w * inv);
        }

        // §6.8。32 bit wrap を許容する。差が 2^31 ms (24.86 日) 未満である前提。
        public static int DeltaInt(int newer, int older)
        {
            return newer - older;
        }

        // max(|a[i] − b[i]|)。変化判定 (§6.3) とテレポート判定 (§6.6) に使う。
        public static long MaxAbsDeltaMm(long[] a, long[] b)
        {
            long dx = a[0] - b[0]; if (dx < 0L) dx = -dx;
            long dy = a[1] - b[1]; if (dy < 0L) dy = -dy;
            long dz = a[2] - b[2]; if (dz < 0L) dz = -dz;
            long m = dx;
            if (dy > m) m = dy;
            if (dz > m) m = dz;
            return m;
        }

        public static bool RotDiffers(short[] a, short[] b)
        {
            return a[0] != b[0] || a[1] != b[1] || a[2] != b[2] || a[3] != b[3];
        }

        public static bool IsZero(Vector3 v)
        {
            return v.x == 0f && v.y == 0f && v.z == 0f;
        }

        public static float Clamp01(float x)
        {
            if (x < 0f) return 0f;
            if (x > 1f) return 1f;
            return x;
        }

        // §9.6 / §4.5 手順 6。整数の clamp。short へ落とす前の範囲確定に使う。
        public static long ClampL(long x, long lo, long hi)
        {
            if (x < lo) return lo;
            if (x > hi) return hi;
            return x;
        }

        // Dot < 0 なら b の符号を反転して最短経路にする (§6.5)。
        public static Quaternion SlerpHemisphere(Quaternion a, Quaternion b, float t)
        {
            float d = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
            if (d < 0f) b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
            return Quaternion.Slerp(a, b, t);
        }

        // dst = origin + QRotate(qOrigin, view × 1000)。§9.6。Manager の AbsFromView の実装本体。
        public static void AbsFromView(long[] origin, Quaternion qOrigin, Vector3 view, long[] dst)
        {
            Vector3 d = QRotate(qOrigin, view * 1000f);
            dst[0] = origin[0] + RoundHalfAwayFromZero(d.x);
            dst[1] = origin[1] + RoundHalfAwayFromZero(d.y);
            dst[2] = origin[2] + RoundHalfAwayFromZero(d.z);
        }

        // Why-not: 絶対値を float にしてから引かない。差を long で先に取る (§7.1)。
        public static Vector3 ViewFromAbs(long[] origin, Quaternion qOrigin, long[] abs)
        {
            Vector3 d = new Vector3(
                (float)(abs[0] - origin[0]) * 0.001f,
                (float)(abs[1] - origin[1]) * 0.001f,
                (float)(abs[2] - origin[2]) * 0.001f);
            return QRotate(QInv(qOrigin), d);
        }

        // §9.3。km セル (1000 m 立方) の索引。絶対 mm 座標を 1 km で割った商。
        // Why-not: 単純な整数除算は負の値で 0 方向へ切り捨てる。床関数にそろえてセルの対称性を保つ。
        public static long KmCellIndex(long absMm)
        {
            long q = absMm / 1000000L;
            long r = absMm - q * 1000000L;
            if (r != 0L && absMm < 0L) q -= 1L;
            return q;
        }

        // §9.3。2 点が同じ km セルか。3 軸すべてでセルの索引が一致するかどうかで判定する。
        public static bool SameKmCell(long[] a, long[] b)
        {
            return KmCellIndex(a[0]) == KmCellIndex(b[0])
                && KmCellIndex(a[1]) == KmCellIndex(b[1])
                && KmCellIndex(a[2]) == KmCellIndex(b[2]);
        }

        // §9.3。station の seated モード (③)。
        // 自分 (Player.local) は false。remote は同じ Root かつ同じ km 圏内のときだけ false。
        public static bool StationSeatedMode(bool isLocalPlayer, bool sameRoot, bool sameKmCell)
        {
            if (isLocalPlayer) return false;
            if (sameRoot && sameKmCell) return false;
            return true;
        }
    }
}
