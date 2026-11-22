using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §6.5。受信サンプルから表示姿勢を推測する純粋計算。状態を持たない。
    // Why-not: FDMiSyncMath に置かない。量子化・座標変換と予測則を同じ型に混ぜると、
    // 予測則だけを EditMode で差し替える境界が消える (§13.1)。
    // Why-not: 外挿を送信間隔で打ち切らない。表示は「最新サンプルからの予測」を常に走らせ、
    // 新しいサンプルが来たら旧軌道と新軌道を混ぜる (ADR-0035)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiSyncPredict : UdonSharpBehaviour
    {
        // §6.5。サンプルが来ない間の扱い。H までは通常の予測、H..H+D は線形減衰、その後は凍結。
        public const int COAST_START_MS = 1000;
        public const int COAST_DECAY_MS = 1000;

        // §6.5。旧/新軌道を混ぜる最大時間。2 Hz プレイヤーでも 500 ms を超えない。
        public const int MAX_BLEND_MS = 500;

        // §6.5。加速度の一次低域通過と、停止扱いにする速度。
        public const float ACCEL_TAU_SEC = 0.25f;
        public const float ACCEL_IDLE_SPEED = 0.5f;

        // §6.5。角速度の上限 [rad/s]。720 deg/s。
        public const float MAX_ANG_VEL_RAD = 12.5663706f;

        // ahead はサンプル stamp からの経過 ms。負は 0 として扱う。
        public static void PredictAbs(long[] p0, Vector3 vel, Vector3 acc, int aheadMs, long[] dst)
        {
            Vector3 d = PredictDeltaMm(vel, acc, aheadMs);
            dst[0] = p0[0] + FDMiSyncMath.RoundHalfAwayFromZero(d.x);
            dst[1] = p0[1] + FDMiSyncMath.RoundHalfAwayFromZero(d.y);
            dst[2] = p0[2] + FDMiSyncMath.RoundHalfAwayFromZero(d.z);
        }

        // p[m] = p0 + v[m/s]·t[ms] + a[m/s²]·t[ms]²/2000。
        // v·t が mm になるのは 1 m/s × 1 ms = 1 mm のため。a は t²/2000 で mm に落ちる。
        public static Vector3 PredictDeltaMm(Vector3 vel, Vector3 acc, int aheadMs)
        {
            if (aheadMs < 0) aheadMs = 0;

            if (aheadMs <= COAST_START_MS)
            {
                float t = (float)aheadMs;
                return new Vector3(
                    vel.x * t + acc.x * t * t / 2000f,
                    vel.y * t + acc.y * t * t / 2000f,
                    vel.z * t + acc.z * t * t / 2000f);
            }

            // H 時点の位置と速度。ここから速度を線形に落とす。
            float h = (float)COAST_START_MS;
            Vector3 pH = new Vector3(
                vel.x * h + acc.x * h * h / 2000f,
                vel.y * h + acc.y * h * h / 2000f,
                vel.z * h + acc.z * h * h / 2000f);
            Vector3 vH = vel + acc * (h / 1000f);

            int s = aheadMs - COAST_START_MS;
            if (s > COAST_DECAY_MS) s = COAST_DECAY_MS;
            float sm = (float)s;
            // vH·s - vH·s²/(2D)。s は ms、vH は m/s なので mm。
            float decay = sm * sm / (2f * (float)COAST_DECAY_MS);
            return new Vector3(
                pH.x + vH.x * sm - vH.x * decay,
                pH.y + vH.y * sm - vH.y * decay,
                pH.z + vH.z * sm - vH.z * decay);
        }

        // 角速度は軸×rad/s。等角速度で外挿する。
        // Why-not: 角速度をクォータニオンで持たない。360°/s のような 1 秒あたりの回転は
        // クォータニオンの最短経路補間で 360° が identity に畳まれ、180° を超える外挿が壊れる。
        public static Quaternion PredictRot(Quaternion q0, Vector3 angVelRadPerSec, int aheadMs)
        {
            if (aheadMs <= 0) return q0;

            if (aheadMs <= COAST_START_MS)
            {
                return ApplyAngVel(q0, angVelRadPerSec, (float)aheadMs / 1000f);
            }

            float h = (float)COAST_START_MS / 1000f;
            Quaternion qH = ApplyAngVel(q0, angVelRadPerSec, h);

            int s = aheadMs - COAST_START_MS;
            if (s > COAST_DECAY_MS) s = COAST_DECAY_MS;
            float sm = (float)s / 1000f;
            float d = (float)COAST_DECAY_MS / 1000f;
            // ∫0^s (1 - τ/D) dτ = s - s²/(2D)。
            float swept = sm - sm * sm / (2f * d);
            return ApplyAngVel(qH, angVelRadPerSec, swept);
        }

        static Quaternion ApplyAngVel(Quaternion q0, Vector3 angVelRadPerSec, float seconds)
        {
            if (seconds <= 0f) return q0;
            float mag = angVelRadPerSec.magnitude;
            if (mag < 1e-6f) return q0;

            float deg = mag * seconds * Mathf.Rad2Deg;
            Vector3 axis = angVelRadPerSec / mag;
            return FDMiSyncMath.QNormalize(Quaternion.AngleAxis(deg, axis) * q0);
        }

        // 区間 dtMs の回転を、軸×rad/s の角速度へ変換する。
        // Why-not: 角速度を同期フィールドにしない。参照 main と同じく姿勢の差分から作る (ADR-0035)。
        public static Vector3 AngVelPerSec(Quaternion q0, Quaternion q1, int dtMs)
        {
            if (dtMs <= 0) return Vector3.zero;

            Quaternion dq = q1 * FDMiSyncMath.QInv(q0);
            if (dq.w < 0f) dq = new Quaternion(-dq.x, -dq.y, -dq.z, -dq.w);

            float angle = 2f * Mathf.Acos(Mathf.Clamp(dq.w, -1f, 1f));
            if (angle < 1e-6f) return Vector3.zero;

            float dt = (float)dtMs / 1000f;
            float maxAngle = MAX_ANG_VEL_RAD * dt;
            if (angle > maxAngle) angle = maxAngle;

            Vector3 axis = new Vector3(dq.x, dq.y, dq.z);
            float len = axis.magnitude;
            if (len < 1e-6f) return Vector3.zero;

            return axis * (angle / (len * dt));
        }

        // 参照 main の bounce 防止。加速方向が反転したらゼロにする。
        // Why-not: 低域通過だけで抑えない。反転をそのまま平滑化すると、行き過ぎた予測を
        // 次のサンプルで戻す挙動が残る (ADR-0035)。
        public static Vector3 SmoothAccel(Vector3 raw, Vector3 prev, int dtMs, Vector3 velocity)
        {
            if (dtMs <= 0) return prev;
            if (velocity.sqrMagnitude < ACCEL_IDLE_SPEED * ACCEL_IDLE_SPEED) return Vector3.zero;
            if (Vector3.Dot(raw, prev) < 0f) return Vector3.zero;

            float dt = (float)dtMs / 1000f;
            float f = dt / (dt + ACCEL_TAU_SEC);
            return prev + (raw - prev) * f;
        }

        public static Vector3 AccelFromVelocities(Vector3 v1, Vector3 v0, int dtMs)
        {
            // Why-not: 1 ms 未満のサンプル間隔で割らない。ジッタで加速度が発散する。
            if (dtMs < 10) return Vector3.zero;
            float inv = 1000f / (float)dtMs;
            return new Vector3((v1.x - v0.x) * inv, (v1.y - v0.y) * inv, (v1.z - v0.z) * inv);
        }

        // 最新サンプル到着からの経過を 1 サンプル区間で正規化する。
        public static float BlendU(int nowMs, int latestArrivalMs, int dtMs)
        {
            if (dtMs <= 0) return 1f;
            if (dtMs > MAX_BLEND_MS) dtMs = MAX_BLEND_MS;

            float u = (float)(nowMs - latestArrivalMs) / (float)dtMs;
            if (u < 0f) return 0f;
            if (u > 1f) return 1f;
            return u;
        }

        // dst は from/to のどちらとも別でも同じでもよい。成分ごとに to を読んでから書く。
        public static void BlendAbs(long[] from, long[] to, float u, long[] dst)
        {
            dst[0] = from[0] + FDMiSyncMath.RoundHalfAwayFromZero((float)(to[0] - from[0]) * u);
            dst[1] = from[1] + FDMiSyncMath.RoundHalfAwayFromZero((float)(to[1] - from[1]) * u);
            dst[2] = from[2] + FDMiSyncMath.RoundHalfAwayFromZero((float)(to[2] - from[2]) * u);
        }
    }
}
