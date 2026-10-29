using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // 地面 (FEF接平面) 基準の姿勢・高度・オイラーを出す表示専用コンポーネント (ADR-0011)。
    // anchor の _localAbs/_localRot を読むだけ。送信・積分・補間には触らない。
    // 機体用とプレイヤー用に1インスタンスずつ anchor と同 GameObject に置く。owner/remote同一式。
    // 高度は h=|p|×0.001−R の float 近似。long→float の ulp (512mm) 由来で約1mの誤差を持つ。
    // q_basis は up=normalize(p) から作る。Editor の FDMiEarth.Basis (double) は Runtime に置けない。
    //
    // Udon 最適化:
    // - eulerAngles は native 1 回 + 正規化3行。Atan2 手書きより手数が少ない。
    // - north=cross(east, up) は正規化しない。単位直交ベクトルの外積は単位ベクトル。
    // Why-not: pitch/roll に生の e.x/e.z を出さない。Unity +X=機首下げ・+Z=右翼上げであり、
    // 航空慣例 (機首上げ+/右翼下げ+) とは符号が逆。ADI反転を防ぐため反転して出す。
    // Why-not: 地形・海面を見ない。真のAGLは地形サンプラの持込みから再検討 (YAGNI)。
    [DefaultExecutionOrder(1100)]
    public class FDMiGroundAttitude : FDMiBehaviour
    {
        [Header("Wiring")]
        public FDMiSyncAnchor anchor;
        public string MagneticDeclinationPath = "~/Physics/MagneticDeclination";

        [FDMiDataPath(nameof(MagneticDeclinationPath))]
        public FDMiFloat MagneticDeclination; // 全機共有の外部入力 [deg]。東偏を正。座標依存なし
        public string AttitudePath = "~/Physics/Attitude";

        [FDMiDataPath(nameof(AttitudePath))]
        public FDMiQuaternion Attitude; // q_rel = QInv(q_basis) ⊗ q_abs。Skybox回転の入力
        public string AltitudePath = "~/Physics/Altitude";

        [FDMiDataPath(nameof(AltitudePath))]
        public FDMiFloat Altitude; // HAE≡MSL (球体・ジオイド差0) [m]。約1m誤差
        public string PitchPath = "~/Physics/Pitch";

        [FDMiDataPath(nameof(PitchPath))]
        public FDMiFloat Pitch; // 機首上げ+ [deg]
        public string RollPath = "~/Physics/Roll";

        [FDMiDataPath(nameof(RollPath))]
        public FDMiFloat Roll; // 右翼下げ+ [deg]
        public string TrueHeadingPath = "~/Physics/HDGTRU";

        [FDMiDataPath(nameof(TrueHeadingPath))]
        public FDMiFloat HDGTRU; // 真北基準 0–360 [deg]
        public string MagneticHeadingPath = "~/Physics/HDGMAG";

        [FDMiDataPath(nameof(MagneticHeadingPath))]
        public FDMiFloat HDGMAG; // HDGTRU−MagneticDeclination を 0–360 化 [deg]

        Quaternion[] _att;
        float[] _h,
            _pitch,
            _roll,
            _tru,
            _mag,
            _dec;

        void Start()
        {
            _att = Attitude.data;
            _h = Altitude.data;
            _pitch = Pitch.data;
            _roll = Roll.data;
            _tru = HDGTRU.data;
            _mag = HDGMAG.data;
            _dec = MagneticDeclination.data;
        }

        // 表示用。DynamicObjectSync (1000) の view 確定後に読む。FixedUpdate 物理と非連動。
        void LateUpdate()
        {
            if (anchor == null)
                return;

            long[] abs = anchor._localAbs;
            Vector3 p = new Vector3((float)abs[0] * 0.001f, (float)abs[1] * 0.001f, (float)abs[2] * 0.001f);
            _h[0] = p.magnitude - 6371000f;

            Vector3 up = p.normalized;
            // east=up⊗Y が FDMiEarth.East と一致する。Y⊗up は −East なので使わない。
            Vector3 eastRaw = Vector3.Cross(up, Vector3.up);
            if (eastRaw.sqrMagnitude < 1e-6f)
                return; // 極特異点。YAGNI のため前回値を保持する。
            Vector3 east = eastRaw.normalized;
            Vector3 north = Vector3.Cross(east, up);
            Quaternion basis = Quaternion.LookRotation(north, up);

            Quaternion rel = FDMiSyncMath.QInv(basis) * anchor._localRot;
            _att[0] = rel;

            Vector3 e = rel.eulerAngles;
            float ex = e.x;
            if (ex > 180f)
                ex -= 360f;
            float ez = e.z;
            if (ez > 180f)
                ez -= 360f;
            _pitch[0] = -ex;
            _roll[0] = -ez;
            float hdg = e.y;
            _tru[0] = hdg;
            // 偏角は ±180 想定で1回正規化する。
            float mag = hdg - _dec[0];
            if (mag < 0f)
                mag += 360f;
            else if (mag >= 360f)
                mag -= 360f;
            _mag[0] = mag;
        }
    }
}
