using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 複数区間の一点空力モデル。V1 (tag 1.0.0 の FDMi_wing) の計算をそのまま持ち込んだ。
    // 区間ごとの対気速度から Cl/Cd/Cm カーブを引き、合力と合モーメントを機体 Rigidbody に渡す。
    //
    // 座標はすべて機体固定 (body ローカル、+Z = 前方、+X = 右)。本コンポーネントの transform は使わない。
    // 区間は coordPos / spanNormal / chordNormal / sectionChord / sectionArea の配列で与える。
    // V1 の Editor が wingTip/wingToe から焼き付けていた値であり、いまは直接編集する。
    //
    // V1 からの変更は入力の受け方だけ。SaccFlight の airData/sharedBool を FDMiFloat/FDMiVector3 に置き換えた。
    // 力の適用は V1 と同じ AddRelativeForce / AddRelativeTorque。区間ごとの作用点は与えず、
    // 空力中心まわりのモーメントを Cm 項と重心オフセットの外積で明示的に組む。
    //
    // 迎え角・横滑り角の微小角近似も失速の切り方も持たない素のテーブル参照モデル。
    // 現行の一点・焼き付けモデルへ寄せていくための土台として置く。
    public class FDMiWing : FDMiBehaviour
    {
        [Header("配線")]
        // 力を加える機体の Rigidbody。
        public Rigidbody body;
        // 風速ベクトル。世界座標。未接続なら無風。単位は m/s。
        public FDMiVector3 windInput;
        // 空気密度。未接続なら rho0 を使う。単位は kg/m^3。
        public FDMiFloat densityInput;
        // 音速。未接続なら sonicSpeed0 を使う。単位は m/s。
        public FDMiFloat sonicSpeedInput;
        // 操舵面。区間ごとの迎え角・係数・面積の加算量をここから読む。
        public FDMiControlSurface[] controlSurface;

        [Header("区間 (body ローカル)")]
        // 空力中心。力の作用点であり、対気速度の測定点でもある。単位は m。
        public Vector3[] coordPos;
        // 翼幅方向の単位ベクトル。区間の気流はこの方向の成分を落とす。
        public Vector3[] spanNormal;
        // 弦方向の単位ベクトル。迎え角の基準。
        public Vector3[] chordNormal;
        // 区間の平均コード長。Cm 項の基準長。単位は m。
        public float[] sectionChord;
        // 区間の面積。単位は m^2。
        public float[] sectionArea;

        [Header("カーブ (区間ごと)")]
        // 2次元揚力係数。迎え角 [deg] に対する値。
        public AnimationCurve[] clAlpha;
        // 2次元抗力係数。
        public AnimationCurve[] cdAlpha;
        // 揚力係数のマッハ補正倍率。
        public AnimationCurve[] clMach;
        // 抗力係数のマッハ補正倍率。
        public AnimationCurve[] cdMach;
        // ピッチングモーメント係数。
        public AnimationCurve[] cmAlpha;

        [Header("設計値")]
        // 面積の全体倍率。V1 の翼面積補正。
        public float wingAreaCoef = 1f;
        // 海面標準大気の密度。densityInput 未接続時の値。単位は kg/m^3。
        public float rho0 = 1.225f;
        // 海面の音速。sonicSpeedInput 未接続時の値。単位は m/s。
        public float sonicSpeed0 = 340.3f;

        [Header("計測値")]
        // 区間の迎え角。操舵面の加算込み。coordPos と同じ長さにする。単位は度。
        public float[] alpha;
        // 直前のステップで加えた合力。機体座標。単位は N。
        public Vector3 totalForce;
        // 直前のステップで加えた合モーメント。機体座標。単位は N m。
        public Vector3 totalMoment;

        void FixedUpdate()
        {
            // 入力。未接続の端子は無風・海面標準として扱う。
            Vector3 wind = Vector3.zero;
            if (windInput != null)
                wind = windInput.Data;
            float rho = rho0;
            if (densityInput != null)
                rho = densityInput.Data;
            float sonicSpeed = sonicSpeed0;
            if (sonicSpeedInput != null)
                sonicSpeed = sonicSpeedInput.Data;

            // 機体座標で見た対気速度と角速度。V1 と同じく「風 - 機体速度」を気流の正方向とする。
            Quaternion invRot = Quaternion.Inverse(body.rotation);
            Vector3 airspeed = invRot * (wind - body.velocity);
            Vector3 airAngularVelocity = invRot * body.angularVelocity;
            Vector3 cogOffset = body.centerOfMass;

            totalForce = Vector3.zero;
            totalMoment = Vector3.zero;
            for (int i = 0; i < coordPos.Length; i++)
            {
                // 操舵面の寄与を区間ごとに合算する。舵角の1次量は加算、面積は積で効かせる。
                float csAlpha = 0f;
                float csCl = 0f;
                float csCd = 0f;
                float csCm = 0f;
                float csArea = 1f;
                for (int j = 0; j < controlSurface.Length; j++)
                {
                    FDMiControlSurface cs = controlSurface[j];
                    csAlpha += cs.alpha[i];
                    csCl += cs.cl[i];
                    csCd += cs.cd[i];
                    csCm += cs.cm[i];
                    csArea *= cs.area[i];
                }

                // 区間の対気速度。並進に回転の誘起速度を足し、スパン方向成分を落とす。
                Vector3 sectionAir = airspeed - Vector3.Cross(airAngularVelocity, coordPos[i]);
                sectionAir = Vector3.ProjectOnPlane(sectionAir, spanNormal[i]);

                alpha[i] = Vector3.SignedAngle(chordNormal[i], sectionAir, spanNormal[i]) + csAlpha;

                // 動圧×面積。コサインは翼面の投影面積。スパン方向の積分はしない。
                float spd = sectionAir.magnitude;
                float c = 0.5f * rho * spd * spd * sectionArea[i] * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * alpha[i]));
                c *= csArea * wingAreaCoef;
                float mach = spd / sonicSpeed;

                // 抗力は気流方向、揚力はそれと直交する向き。
                Vector3 cdNormal = Vector3.Normalize(sectionAir);
                Vector3 clNormal = Vector3.Cross(spanNormal[i], cdNormal);

                Vector3 cForce = (clAlpha[i].Evaluate(alpha[i]) + csCl) * clMach[i].Evaluate(mach) * clNormal
                    + (cdAlpha[i].Evaluate(alpha[i]) + csCd) * cdMach[i].Evaluate(mach) * cdNormal;
                totalForce += c * cForce;
                totalMoment += c * (-sectionChord[i] * ((cmAlpha[i].Evaluate(alpha[i]) + csCm) * spanNormal[i])
                    + Vector3.Cross(coordPos[i] - cogOffset, cForce));
            }
            body.AddRelativeForce(totalForce);
            body.AddRelativeTorque(totalMoment);
        }
    }
}
