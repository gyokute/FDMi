using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §6.5。受信サンプルから表示姿勢を推測する純粋計算。状態を持たない。
    // Why-not: FDMiSyncMath に置かない。量子化・座標変換と予測則を同じ型に混ぜると、
    // 予測則だけを EditMode で差し替える境界が消える (§13.1)。
    // Why-not: 外挿を送信間隔で打ち切らない。表示は「最新サンプルからの予測」を常に走らせる。
    // Why-not: 旧/新軌道を角度で混ぜない。平滑化した速度で積分した smooth を raw へ補正する (ADR-0036)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiSyncPredict : UdonSharpBehaviour
    {
        // §6.5。サンプルが来ない間の扱い。H までは通常の予測、H..H+D は線形減衰、その後は凍結。
        public const int COAST_START_MS = 1000;
        public const int COAST_DECAY_MS = 1000;

        // §6.5。SAV 型の raw/smooth。位置と姿勢を別々の時定数で平滑化する。
        public const float POS_CORRECTION_PER_SEC = 8f;
        public const float POS_SPEED_LERP_PER_SEC = 4f;
        public const float POS_DERIV_STRENGTH = 1.666666f;
        public const float ROT_SPEED_LERP_PER_SEC = 10f;
        public const float ROT_CORRECTION_PER_SEC = 1f;

        // §6.5。加速度の一次低域通過と、停止扱いにする速度。
        public const float ACCEL_TAU_SEC = 0.25f;
        public const float ACCEL_IDLE_SPEED = 0.5f;

        // §6.5。角速度・角加速度の上限。どちらも 720 deg/s, 720 deg/s²。
        public const float MAX_ANG_VEL_RAD = 12.5663706f;
        public const float MAX_ANG_ACC_RAD = 12.5663706f;

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

        // 位置予測の瞬時速度 [m/s]。PredictDeltaMm の微分であり、平滑化の目標に使う。
        public static Vector3 PredictVel(Vector3 vel, Vector3 acc, int aheadMs)
        {
            if (aheadMs < 0) aheadMs = 0;
            if (aheadMs <= COAST_START_MS)
                return vel + acc * ((float)aheadMs / 1000f);

            Vector3 vH = vel + acc * ((float)COAST_START_MS / 1000f);
            int s = aheadMs - COAST_START_MS;
            if (s > COAST_DECAY_MS) s = COAST_DECAY_MS;
            return vH * (1f - (float)s / (float)COAST_DECAY_MS);
        }

        // 角速度は軸×rad/s。等角速度で外挿する。
        // Why-not: 角速度をクォータニオンで持たない。360°/s のような 1 秒あたりの回転は
        // クォータニオンの最短経路補間で 360° が identity に畳まれ、180° を超える外挿が壊れる。
        public static Quaternion PredictRot(Quaternion q0, Vector3 angVelRadPerSec, int aheadMs)
        {
            return PredictRotAccel(q0, angVelRadPerSec, Vector3.zero, aheadMs);
        }

        // 角速度と角加速度から姿勢を外挿する。角加速度は回転ベクトルの二次項として積分する。
        // 減速が回転軸と揃っているときは、角速度がゼロになる時刻で止める。
        // Why-not: SAV のクォータニオン角加速度を逐語移植しない。FDMi は軸×rad/s の
        // ベクトル角速度を既に持っており、ベクトルの方がテストと UdonSharp の見通しがよい (ADR-0036)。
        public static Quaternion PredictRotAccel(
            Quaternion q0, Vector3 angVelRadPerSec, Vector3 angAccRadPerSec2, int aheadMs)
        {
            if (aheadMs <= 0) return q0;

            float total = (float)aheadMs / 1000f;
            float stop = StopTime(angVelRadPerSec, angAccRadPerSec2);
            if (stop >= 0f && stop <= total)
            {
                Vector3 stopVec = angVelRadPerSec * stop + angAccRadPerSec2 * (0.5f * stop * stop);
                return ApplyRotVec(q0, stopVec);
            }

            float h = (float)COAST_START_MS / 1000f;
            float t = (total < h) ? total : h;
            Vector3 rotVec = angVelRadPerSec * t + angAccRadPerSec2 * (0.5f * t * t);
            Quaternion q = ApplyRotVec(q0, rotVec);
            if (total <= h) return q;

            Vector3 wH = angVelRadPerSec + angAccRadPerSec2 * h;
            int s = aheadMs - COAST_START_MS;
            if (s > COAST_DECAY_MS) s = COAST_DECAY_MS;
            float sm = (float)s / 1000f;
            float d = (float)COAST_DECAY_MS / 1000f;
            float swept = sm - sm * sm / (2f * d);
            return ApplyAngVel(q, wH, swept);
        }

        // 平滑化の目標にする角速度の瞬時値。角加速度とコーストを含む。
        public static Vector3 PredictAngVel(Vector3 omega0, Vector3 alpha, int aheadMs)
        {
            if (aheadMs < 0) aheadMs = 0;

            float total = (float)aheadMs / 1000f;
            float stop = StopTime(omega0, alpha);
            if (stop >= 0f && stop <= total) return Vector3.zero;

            if (aheadMs <= COAST_START_MS)
                return ClampMag(omega0 + alpha * total, MAX_ANG_VEL_RAD);

            Vector3 wH = omega0 + alpha * ((float)COAST_START_MS / 1000f);
            int s = aheadMs - COAST_START_MS;
            if (s > COAST_DECAY_MS) s = COAST_DECAY_MS;
            float f = 1f - (float)s / (float)COAST_DECAY_MS;
            return ClampMag(wH * f, MAX_ANG_VEL_RAD);
        }

        // 減速が回転軸方向に揃っているときだけ、角速度がゼロになる時刻を返す。
        // Why-not: 任意の 3D 角加速度でゼロ交差を扱わない。軸がずれた減速は「停止」ではなく
        // 向きの変化であり、回転を凍結すると別の不自然が出る。
        static float StopTime(Vector3 omega0, Vector3 alpha)
        {
            float speed = omega0.magnitude;
            if (speed < 1e-6f) return -1f;

            float along = Vector3.Dot(omega0, alpha) / speed;
            if (along >= -1e-6f) return -1f;

            Vector3 perp = alpha - omega0 * (Vector3.Dot(omega0, alpha) / (speed * speed));
            if (perp.sqrMagnitude > 0.01f * alpha.sqrMagnitude) return -1f;

            return -speed / along;
        }

        public static Vector3 ClampMag(Vector3 v, float max)
        {
            float m = v.magnitude;
            if (m > max && m > 1e-6f) return v * (max / m);
            return v;
        }

        public static Quaternion ApplyAngVel(Quaternion q0, Vector3 angVelRadPerSec, float seconds)
        {
            if (seconds <= 0f) return q0;
            return ApplyRotVec(q0, angVelRadPerSec * seconds);
        }

        static Quaternion ApplyRotVec(Quaternion q0, Vector3 rotVecRad)
        {
            float angle = rotVecRad.magnitude;
            if (angle < 1e-6f) return q0;
            Vector3 axis = rotVecRad / angle;
            return FDMiSyncMath.QNormalize(Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis) * q0);
        }

        // 区間 dtMs の回転を、軸×rad/s の角速度へ変換する。
        // Why-not: 角速度を同期フィールドにしない。参照 main と同じく姿勢の差分から作る (ADR-0036)。
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
        // 次のサンプルで戻す挙動が残る (ADR-0036)。
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

        // raw と現在の表示位置の差 [m]。
        public static Vector3 PositionErrorM(long[] rawMm, long[] poseMm)
        {
            return new Vector3(
                (float)(rawMm[0] - poseMm[0]) * 0.001f,
                (float)(rawMm[1] - poseMm[1]) * 0.001f,
                (float)(rawMm[2] - poseMm[2]) * 0.001f);
        }

        // SAV の Deriv。誤差が縮んでいるときだけ、補正の行き過ぎを抑える速度 [m/s] を返す。
        public static Vector3 PositionDeriv(Vector3 errorM, float lastErrorM, int dtMs)
        {
            if (dtMs <= 0) return Vector3.zero;
            float error = errorM.magnitude;
            if (error >= lastErrorM) return Vector3.zero;
            if (error < 1e-6f) return Vector3.zero;

            float dt = (float)dtMs / 1000f;
            float scale = (lastErrorM - error) * POS_DERIV_STRENGTH / (error * dt);
            return -errorM * scale;
        }

        // 一次低域通過。位置速度と角速度の両方に使う。
        public static Vector3 SmoothVec(Vector3 prev, Vector3 target, float perSec, int dtMs)
        {
            if (dtMs <= 0) return prev;
            float f = (float)dtMs / 1000f * perSec;
            if (f > 1f) f = 1f;
            return prev + (target - prev) * f;
        }
    }
}
