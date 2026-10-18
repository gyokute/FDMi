using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // カプセル近似の胴体空力。旅客機の与圧胴体 (円断面の細長い回転体) を、空力中心1点の合力に縮退させる。
    // 胴体軸が気流に対して傾くと、傾きに応じた垂直力が出る。機首上げなら上向き、横滑りなら横向き。
    // ランタイムで使う項は垂直力と軸力の2つだけで、三角関数も区間積分も要らない。
    //
    // 座標はすべて機体固定 (body ローカル、+Z = 前方、+X = 右、+Y = 上)。本コンポーネントの transform は使わない。
    // 対気速度は FDMiAirData.AirSpeed と同じ「機体速度 - 風」とする。翼 (V1 移植) の「風 - 機体速度」とは符号が逆。
    //
    // 垂直力は DATCOM の body-alone 推算 (Jorgensen のクロスフロー法) の縮退。円断面の細長体では
    //   CN = 2 sinα cosα + Cdc (Ap/Smax) sin²α
    // 第1項はスレンダーボディ理論のポテンシャル項 (CNα = 2 /rad)、第2項は粘性クロスフロー項。
    // α を速度成分で書き直すと、垂直力は「垂直成分を打ち消す抗力」1項にまとまる。
    //   a = u・軸,  u⊥ = u - 軸 a,  p = |u⊥|
    //   F⊥ = -u⊥ ρ (Smax a + 0.5 Cdc Ap p)
    // 傾きと同じ向きに揚力が出るのではなく、傾きで生じた横流れを打ち消す向きに出る。結果として
    // 上を向いた姿勢なら上向き、横向きの姿勢なら横向きの力になり、迎角と横滑りで同じ式が効く。
    //
    // 軸力の等価抗力面積は 2.x の FDMiDragBody (NAL-SP-00012) の式をそのまま使う。
    //   cdA = 0.5 (cdp + λ l/d) π d,  Drag = 0.5 ρ V² cdA
    // 2.x は Drag = ρ V² cdA としていたが、等価抗力面積の定義どおり標準の動圧に揃えた。
    //
    // 区間分割はしない。DATCOM の body-alone も最終的には合力と CP に縮退する。
    // 力の作用点は centerOfPressure 1点。重心回りのモーメントは作用点のずれから出る。
    // 90 度を超える迎角では第1項の符号が反転するが、旅客機の通常飛行では起こらないので扱わない。
    public class FDMiBodyAero : FDMiBehaviour
    {
        [Header("配線")]
        // 力を加える機体の Rigidbody。
        public Rigidbody body;
        // 風速ベクトル。世界座標。未接続なら無風。単位は m/s。
        public FDMiVector3 windInput;
        // 空気密度。未接続なら rho0 を使う。単位は kg/m^3。
        public FDMiFloat densityInput;

        [Header("形状 (カプセル)")]
        // 胴体軸の向き。機首方向。body ローカルの単位ベクトル。
        public Vector3 axis = Vector3.forward;
        // 全長。単位は m。
        public float length = 38f;
        // 最大直径。基準面積 Smax = π d^2/4 と平面形面積 Ap = d l に効く。単位は m。
        public float diameter = 4f;
        // 合力の作用点 (CP)。body ローカル。単位は m。
        public Vector3 centerOfPressure = Vector3.zero;

        [Header("係数")]
        // クロスフロー抗力係数 Cdc。円断面の亜音速の目安は 1.2。調整開始値。
        public float crossflowCd = 1.2f;
        // 圧力抗力係数。NAL-SP-00012 の cdp。調整開始値。
        public float cdp = 0.075f;
        // 摩擦抗力の細長比項。NAL-SP-00012 の λ。調整開始値。
        public float lambda = 0.016f;
        // 海面標準大気の密度。densityInput 未接続時の値。単位は kg/m^3。
        public float rho0 = 1.225f;

        [Header("計測値")]
        // 胴体軸に対する迎角。度。
        public float alphaDeg;
        // 垂直力係数。Smax 基準。
        public float CN;
        // 揚力係数。風軸。Smax 基準。
        public float CL;
        // 抗力係数。風軸。Smax 基準。
        public float CD;
        // 直前のステップで加えた合力。機体座標。単位は N。
        public Vector3 totalForce;
        // 直前のステップで加えた合モーメント。機体座標。単位は N m。
        public Vector3 totalMoment;

        // 基準面積、粘性クロスフロー項の係数、等価抗力面積。形状から一度だけ求める。
        // 焼き付け用の Editor を分けるほどの量ではないので Start に置く。
        float _smax;
        float _crossflowK;
        float _cdA;

        void Start()
        {
            _smax = 0.25f * Mathf.PI * diameter * diameter;
            _crossflowK = 0.5f * crossflowCd * diameter * length;
            _cdA = 0.5f * (cdp + lambda * length / diameter) * (Mathf.PI * diameter);
        }

        void FixedUpdate()
        {
            // 入力。未接続の端子は無風・海面標準として扱う。
            Vector3 wind = Vector3.zero;
            if (windInput != null)
                wind = windInput.Data;
            float rho = rho0;
            if (densityInput != null)
                rho = densityInput.Data;

            // 空力中心の対気速度。並進に回転の誘起速度を足す。機体速度 - 風。
            Quaternion invRot = Quaternion.Inverse(body.rotation);
            Vector3 u = invRot * (body.velocity - wind) + Vector3.Cross(invRot * body.angularVelocity, centerOfPressure);

            // 停機・微速では力も係数も出さずに帰る。ゼロ割り防止も兼ねる。
            float v2 = u.sqrMagnitude;
            if (v2 < 0.25f)
            {
                alphaDeg = 0f;
                CN = 0f;
                CL = 0f;
                CD = 0f;
                totalForce = Vector3.zero;
                totalMoment = Vector3.zero;
                return;
            }

            // 軸方向成分 a と垂直成分 u⊥。垂直力は u⊥ を打ち消す向きに出る。
            float a = Vector3.Dot(u, axis);
            Vector3 perp = u - axis * a;
            float p = perp.magnitude;

            // 動圧。軸力と係数の基準。
            float q = 0.5f * rho * v2;
            // 垂直力。ポテンシャル項と粘性クロスフロー項を1項にまとめた形。
            Vector3 force = perp * (-rho * (_smax * a + _crossflowK * p));
            // 軸力。摩擦と底面抗力。
            force -= axis * (q * _cdA);

            totalForce = force;
            totalMoment = Vector3.Cross(centerOfPressure - body.centerOfMass, force);
            body.AddRelativeForce(force);
            body.AddRelativeTorque(totalMoment);

            // 計測値。風軸の揚力・抗力は body 軸の垂直力・軸力を α で回して出す。
            float invSpeed = 1f / u.magnitude;
            float invQS = 1f / (q * _smax);
            float cn = rho * (_smax * a + _crossflowK * p) * p * invQS;
            float ca = _cdA / _smax;
            CN = cn;
            CL = (cn * a - ca * p) * invSpeed;
            CD = (cn * p + ca * a) * invSpeed;
            alphaDeg = Mathf.Atan2(p, a) * Mathf.Rad2Deg;
        }
    }
}
