using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.dynamics
{
    // 台形翼1枚分の一点空力モデル。翼面上の代表点(空力中心)の対気速度から揚力・抗力・横力を求め、
    // 同一 Rigidbody へ FixedUpdate ごとに1回だけ加える。
    // 本コンポーネントの transform が翼の姿勢を表す。transform.forward は前縁向きの弦方向、
    // transform.right は右翼端向きの翼幅方向、transform.up は翼上面の法線方向とする。
    // 空力中心の位置は acPoint に置く。AddForceAtPosition で加えるため、重心回りのモーメントは
    // 力の作用点をずらすだけで自然に生まれる。モーメント係数の計算は行わない。
    //
    // 翼-胴体干渉と上流翼からの吹き下ろしは、Editor で焼き付けた係数に縮退させる。
    // 干渉の幾何学的な部分は形が変わらない限り計算し直す必要がないため、実行時には掛け算だけ残す。
    // スパン方向の積分は行わない。翼ごとに1点計算と決めることで、Udon でも翼数×50 Hz で回る。
    //
    // 力の実行はオーナークライアントで行う。他の Rigidbody 駆動モジュールと同じ運用とする。
    // 単位はすべて SI。角度の入出力は度、内部の三角計算のみラジアンとする。
    public class FDMiWing : FDMiBehaviour
    {
        [Header("配線")]
        // 力を加える機体の Rigidbody。
        public Rigidbody body;
        // 空力中心。力の作用点であり、対気速度の測定点でもある。
        public Transform acPoint;
        // 主操舵の等価舵角。度。正は後縁下げ(揚力増加向き)。
        public FDMiFloat controlInput;
        // 高揚力フラップの舵角。度。正は後縁下げ。
        public FDMiFloat flapInput;
        // 制動板の開度。0 が全閉、1 が全開。
        public FDMiFloat brakeInput;
        // 空気密度。未接続なら rho0 を使う。単位は kg/m^3。
        public FDMiFloat densityInput;
        // 風速ベクトル。未接続なら無風。単位は m/s。
        public FDMiVector3 windInput;
        // 上流翼。主翼に対する尾翼のように、吹き下ろしを受ける側だけが指定する。
        // 指定した側は相手の前回ステップの揚力係数を読み、迎え角を減らす。
        public FDMiWing sourceWing;

        [Header("設計値")]
        // 翼幅。単位は m。
        [Range(0.5f, 80f)]
        public float span = 10f;
        // 翼根弦長。単位は m。
        [Min(0.05f)]
        public float rootChord = 2f;
        // 翼端弦長。単位は m。
        [Min(0.05f)]
        public float tipChord = 1f;
        // 25% 弦の後退角。単位は度。
        [Range(0f, 60f)]
        public float sweepDeg25 = 0f;
        // 取付角。前縁上げを正とする。単位は度。
        [Range(-10f, 10f)]
        public float incidenceDeg = 2f;
        // 上反角。単位は度。
        [Range(-10f, 30f)]
        public float dihedralDeg = 5f;
        // 胴体直径。翼-胴体干渉の持上げ量に使う。単位は m。
        [Min(0f)]
        public float bodyDiameter = 2f;
        // 2次元翼型の零揚力角。単位は度。
        [Range(-10f, 10f)]
        public float airfoilAlpha0Deg = -2f;
        // 2次元翼型の最大揚力係数。
        [Range(0.5f, 3f)]
        public float airfoilClMax = 1.4f;
        // 2次元翼型の最小抗力係数。
        [Min(0f)]
        public float airfoilCd0 = 0.006f;
        // 操舵面のコード比。0 がなし、1 が全弦。
        [Range(0f, 0.6f)]
        public float controlChordRatio = 0.25f;
        // 操舵面のスパン比。0 がなし、1 が全幅。
        [Range(0f, 1f)]
        public float controlSpanFraction = 0.3f;
        // フラップのコード比。
        [Range(0f, 0.6f)]
        public float flapChordRatio = 0.25f;
        // フラップのスパン比。
        [Range(0f, 1f)]
        public float flapSpanFraction = 0.6f;
        // 制動板の面積比。翼面積に対する比。
        [Range(0f, 0.2f)]
        public float brakeAreaRatio = 0.02f;
        // 上流翼の空力中心から自翼の空力中心までの前後距離。尾翼だけが使う。単位は m。
        [Min(0f)]
        public float tailArmM = 5f;
        // 上流翼の空力中心から自翼の空力中心までの上下差。尾翼だけが使う。単位は m。
        [Min(0f)]
        public float tailHeightM = 0f;
        // 海面標準大気の密度。densityInput 未接続時の値。単位は kg/m^3。
        public float rho0 = 1.225f;

        [Header("焼き付け済み係数")]
        // Editor の焼き付けボタンが代入する。実行時は読み出すだけで書き換えない。
        public float bakedArea = 15f;
        public float bakedLiftSlope = 4.5f;
        public float bakedAlpha0Rad = -0.07f;
        public float bakedCLmax = 1.4f;
        public float bakedCLmin = -1.1f;
        public float bakedCD0 = 0.014f;
        public float bakedK = 0.05f;
        public float bakedCyBeta = -0.05f;
        public float bakedClControl = 1f;
        public float bakedClFlap = 1.5f;
        public float bakedCdFlap = 0.7f;
        public float bakedCdBrake = 0.02f;
        public float bakedClBrakeLoss = 0.006f;
        public float bakedDownwashGain = 0f;
        public float bakedEtaQ = 1f;
        public float bakedWakeLoss = 0f;
        public float bakedStallGain = 1.5f;

        [Header("計測値")]
        // 下流翼が読むための公開値でもある。読み取り専用のつもりで扱う。
        public float outCL;
        public float outCD;
        public float outAlphaDeg;
        public float outQ;
        public float outLift;
        public float outDrag;
        public float outEpsilonDeg;
        public float outBrake;

        // 海面の音速。圧縮性補正だけに使う。単位は m/s。
        const float _soundSpeed = 340.3f;
        // 音速の逆数。実行時の割り算を掛け算に変える。
        const float _invSoundSpeed = 0.002938669f;
        // 度とラジアンの換算。実行時の割り算を掛け算に変える。
        const float _degToRad = 0.0174532925f;
        const float _radToDeg = 57.29578f;

        void FixedUpdate()
        {
            // 入力を読む。未接続の端子は無風・中立として扱う。
            float controlDeg = 0f;
            if (controlInput != null)
                controlDeg = controlInput.Data;
            float flapDeg = 0f;
            if (flapInput != null)
                flapDeg = flapInput.Data;
            float brake = 0f;
            if (brakeInput != null)
                brake = brakeInput.Data;
            if (brake > 1f)
                brake = 1f;
            else if (brake < 0f)
                brake = 0f;
            float rho = rho0;
            if (densityInput != null)
                rho = densityInput.Data;
            Vector3 wind = Vector3.zero;
            if (windInput != null)
                wind = windInput.Data;

            // 代表点の対気速度。回転による誘起速度も GetPointVelocity に含まれる。
            Vector3 ac = acPoint.position;
            Vector3 v = body.GetPointVelocity(ac) - wind;

            // 機軸ではなく翼軸で分解する。翼が捻じれていてもこの翼面の迎え角になる。
            Vector3 fwd = transform.forward;
            Vector3 up = transform.up;
            Vector3 rgt = transform.right;
            float u = Vector3.Dot(v, fwd);
            float w = Vector3.Dot(v, up);
            float s = Vector3.Dot(v, rgt);

            // 動圧。微速では力を出さずに帰る。ゼロ割り防止も兼ねる。
            float v2 = u * u + w * w + s * s;
            if (v2 < 0.25f)
            {
                PublishTelemetry(0f, 0f, 0f, 0f, 0f, 0f, 0f, brake);
                return;
            }
            float spd = Mathf.Sqrt(v2);
            float invV = 1f / spd;
            float q = 0.5f * rho * v2;

            // 揚力傾斜の圧縮性補正。プラントル・グラワート則の近似。
            float mach = spd * _invSoundSpeed;
            float mc2 = 1f - mach * mach;
            if (mc2 < 0.2f)
                mc2 = 0.2f;
            float slope = bakedLiftSlope / Mathf.Sqrt(mc2);

            // 迎え角と横滑り角。横滑り角は微小角近似で割り算だけにする。
            float alpha = Mathf.Atan2(w, -u);
            float beta = s * invV;
            if (beta > 0.5f)
                beta = 0.5f;
            else if (beta < -0.5f)
                beta = -0.5f;

            // 上流翼の吹き下ろしと後流欠損。前回ステップの値を読む。1段遅れは許容する。
            float srcCL = 0f;
            float srcBrake = 0f;
            if (sourceWing != null)
            {
                srcCL = sourceWing.outCL;
                srcBrake = sourceWing.outBrake;
            }
            float eps = bakedDownwashGain * srcCL;
            float eta = bakedEtaQ - bakedWakeLoss * srcBrake;
            if (eta < 0.3f)
                eta = 0.3f;

            // 揚力係数。直線部を求めてから失速上限で切る。はみ出しは抗力増分に回す。
            float controlRad = controlDeg * _degToRad;
            float flapRad = flapDeg * _degToRad;
            float clLin = slope * (alpha - eps - bakedAlpha0Rad)
                + bakedClControl * controlRad
                + bakedClFlap * flapRad
                - bakedClBrakeLoss * brake;
            float cl = clLin;
            if (cl > bakedCLmax)
                cl = bakedCLmax;
            else if (cl < bakedCLmin)
                cl = bakedCLmin;
            float excess = clLin - cl;

            // 抗力係数。誘導抗力に装置抗力と失速増分を足す。
            float qEff = q * eta;
            float fDyn = qEff * bakedArea;
            float cd = bakedCD0 + bakedK * cl * cl
                + bakedCdFlap * flapRad * flapRad
                + bakedCdBrake * brake
                + bakedStallGain * excess * excess;

            // 揚力方向は速度に垂直で翼面対称面内の向き。抗力は速度と逆向き。
            Vector3 vHat = v * invV;
            float d = Vector3.Dot(up, vHat);
            Vector3 liftDir = up - vHat * d;
            float n2 = Vector3.Dot(liftDir, liftDir);
            if (n2 < 1e-6f)
                n2 = 1e-6f;
            liftDir = liftDir / Mathf.Sqrt(n2);
            float cy = bakedCyBeta * beta;
            Vector3 force = liftDir * (fDyn * cl) - vHat * (fDyn * cd) + rgt * (fDyn * cy);
            body.AddForceAtPosition(force, ac, ForceMode.Force);

            PublishTelemetry(cl, cd, alpha * _radToDeg, qEff, fDyn * cl, fDyn * cd, eps * _radToDeg, brake);
        }

        // 計測値をまとめて出す。下流翼が読む outCL と outBrake もここで更新する。
        void PublishTelemetry(float cl, float cd, float alphaDeg, float qEff, float lift, float drag, float epsDeg, float brake)
        {
            outCL = cl;
            outCD = cd;
            outAlphaDeg = alphaDeg;
            outQ = qEff;
            outLift = lift;
            outDrag = drag;
            outEpsilonDeg = epsDeg;
            outBrake = brake;
        }
    }
}
