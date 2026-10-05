using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.dynamics
{
    // 非駆動の旅客機用ホイール単体: 1 Raycast + バイリニア・バネダンパストラット + スリップベースタイヤ + キネマチック回転 + ABS。
    // WheelCollider も接地用コライダーも不要。ホイールごとに本コンポーネントを1つ追加し、すべて同一 Rigidbody に作用させる。
    // 本コンポーネントは脚上端に配置: transform.position = o (脚上端)、transform.up = a (脚上方向)、transform.forward = 舵角ゼロ時の前方向。
    // 力は接地点 p に ForceMode.Force で、FixedUpdate ごとにホイール1輪につき1回加える。
    // 離着陸・タキシングのオーナーシップ: 他のオーナー駆動 Rigidbody 力と同様に VRChat のオーナークライアントで実行する。
    //
    // 可視化は外部の責務。本コンポーネントは Transform を一切動かさない。
    // サス縮み (正規化 0..1) とタイヤ回転量 (ラジアン累積 0..2pi ラップ) は FDMiFloat で出し、
    // 外部の FDMiTransformMove / FDMiTransformRotate 等でタイヤ・脚カバー・SteerPivot を駆動する。
    // SteerPivot 自体の回転もここでは扱わない。舵角は接地フレームの力の向きにだけ使う。
    //
    // ストラットはバイリニア・バネダンパ。折点までは基本レート k0、以深は末端レート kEnd の2段バネに、
    // 圧側/伸側で別値の線形減衰を組み合わせる。旧オレオ・ニューマチック式は議論の結果 B 案で置換した:
    // 150〜250t 級でも脚固有振動数は約 1Hz (dt*ω ~ 0.1) で陽解法の余裕は大きく、安定性は上限・クランプ側が担う。
    // オレオのプログレッシブ特性はハードランディングのエネルギー吸収用であり、その役割は末端レートが引き継ぐ。
    // 静姿勢は k0 = 静荷重 / staticStroke で定義通りに決まり、ガスプリロード解法は不要。Pow は焼き付けにも実行時にもない。
    //
    // 100〜200t 級の旅客機に WheelCollider を使わない理由:
    // - サス/ダンパ調整が跳ねるか死ぬかの両極端になりがちで、駆動輪モデルが 0 m/s 付近で外力と競合する
    //   (プッシュだけではタキシングせず、無理に押すと突然タイヤが高速回転する)。
    // - 旅客機タイヤはトルクで駆動されるのではなく地面に転がされるため、回転はここではキネマチックに解く:
    //   剛いタイヤ特性に対するトルク積分ではなく、omega が vx * (1 - bEff) / R に小さな遅れで追従する。
    //   これにより剛い ODE (と慣性/トルクパラメータ) を除去し、0 m/s での挙動を自明に安定化する:
    //   非制動時の Fx は ~0 のため外力で機体を押せ、制動時はクリープ平衡で保持する
    //   (Fx ~= -Fz * muPeak * vx / (v0 * kappaPeak): slipSpeedScale v0 が駐機保持の剛性ノブ)。
    //
    // docs/DynamicsModel/Wheel に対するレビュー&判定 (他AIドラフト v1): ACCEPT=接地フレーム、
    // 2次オリフィス減衰、バイリニア mu + 微小角横力 + L1 合成クランプ、低速サスペンド付きヒステリシス ABS。
    // REJECT=トルク積分スピン部 (Tmax/I/lambda/安定クランプ): 50 Hz FixedUpdate で剛い車輪速 ODE を復活させ、
    // 調整困難な SI パラメータを2つ増やすだけで、機体が感じる効果はない
    // (車輪慣性 << 機体質量; スピンアップ時定数 ~0.1 s は spinFollowRate で再現済み)。Sqrt 摩擦楕円も実行時は REJECT
    // (L1 を採用: 45 deg 複合時のみ最大29%保守的になるが、旅客機はその領域に留まらない)。
    //
    // 調整順序 (すべて SI): まず静止姿勢用に輪荷重分担 loadShare wi (合計 = 1) + staticStroke xs
    // -> 乗り心地・接地用に zetaCompress/zetaRebound (臨界減衰比) + dampSpeedMax ->
    // 着陸用に kneeFraction/endRateFactor (+ 脚上限 maxLoadFactor) ->
    // スリップ用に muPeak/muSlide/kappaPeak/muLateral/alphaPeakDeg -> ABS 用にブレーキレート + absLowSlip/absHighSlip。
    // Inspector の Range が入力値の検証を担う。構造的不変条件 (折点が静姿勢より上、ABS 閾値順序、静荷重由来の派生量) は
    // Editor の焼き付けが正規化し、Start では焼き付け済み係数を読み出すだけにする。未焼き付けの旧シーン救済のフォールバックのみ残す。
    //
    // Udon コスト対策 (FixedUpdate・接地時):
    // - Pow なし (ガスバネ廃止)。Sqrt なし (安価な L1 合成クランプ)。Atan なし (vy/vx は微小角近似)。
    // - transform.up / hit.normal は既に単位ベクトル、t = Cross(n, f) も単位ベクトルのため normalize 3回を削減。
    //   Sqrt はフレーム投影の normalize 1回のみ。
    // - 逆数・バネ・減衰は焼き付けで事前計算: 実行時の割り算は kappa + cap-scale のみ。バネは分岐+掛け算。
    // - 因果順序は strut -> ABS -> spin -> tire: ブレーキ指令は余分な1ステップ遅れなく同ステップで Fx に届く。
    // - The probe ray ignores triggers (cockpit/pen UI triggers must not snag it).
    // - Damping uses stroke rate clamped to dampSpeedMax (explicit-Euler safety),
    //   and total leg force is capped at maxLoadFactor * static share (bottoming-stop proxy).
    // - Rigidbody requirements: non-kinematic, realistic inertia tensor (order m*L^2/12 per axis; the unit
    //   tensor (1,1,1) on a 100 t body explodes on the first off-center contact), sum of loadShare = 1.
    public class FDMiWheelCollider : FDMiBehaviour
    {
        [Header("Wiring")]
        public Rigidbody body;
        public FDMiFloat steerInput;
        public FDMiFloat brakeInput;
        public LayerMask runwayMask;
        // サス縮み出力。正規化 0..1 (0=全伸び、1=底付き)。接地なしでは 0。外部の可視化・脚カバー駆動用。
        public FDMiFloat suspensionOutput;
        // タイヤ回転量出力。ラジアン累積 0..2pi ラップ。単位不問の前提で外部の回転駆動用。
        public FDMiFloat spinOutput;

        [Header("Geometry")]
        [Range(0.05f, 2f)]
        public float maxStroke = 0.5f;
        [Range(0.05f, 1.5f)]
        public float tireRadius = 0.5f;
        [Range(0f, 90f)]
        public float steerLimitDeg = 70f;

        [Header("Spring-damper strut (bilinear)")]
        [Min(0f)]
        public float staticStroke = 0.3f;
        [Range(0.001f, 1f)]
        public float loadShare = 0.1f;
        // 臨界減衰比。圧側。目安 0.2〜0.5。
        [Range(0.05f, 2f)]
        public float zetaCompress = 0.3f;
        // 臨界減衰比。伸側。目安 0.3〜0.8 (圧側の約1.5倍)。
        [Range(0.05f, 2f)]
        public float zetaRebound = 0.5f;
        [Range(1f, 20f)]
        public float dampSpeedMax = 8f;
        [Range(1f, 20f)]
        public float maxLoadFactor = 6f;
        // 折点の位置。maxStroke に対する比。静姿勢より下にはならない (焼き付けで自動補正)。
        [Range(0f, 1f)]
        public float kneeFraction = 0.7f;
        // 末端レート。基本レート k0 に対する倍率。1 でフルリニア。
        [Range(1f, 8f)]
        public float endRateFactor = 3f;

        [Header("Tire")]
        [Range(0.05f, 2f)]
        public float muPeak = 0.8f;
        [Range(0f, 2f)]
        public float muSlide = 0.5f;
        [Range(0.01f, 0.9f)]
        public float kappaPeak = 0.12f;
        [Range(0.05f, 2f)]
        public float muLateral = 0.9f;
        [Range(0.5f, 45f)]
        public float alphaPeakDeg = 10f;
        [Range(0f, 1f)]
        public float kappaCoupling = 0.5f;
        [Range(0.05f, 10f)]
        public float slipSpeedScale = 1f;

        [Header("Brake / spin / ABS")]
        [Range(0.5f, 50f)]
        public float spinFollowRate = 10f;
        [Range(0.01f, 1f)]
        public float absLowSlip = 0.08f;
        [Range(0.02f, 1f)]
        public float absHighSlip = 0.15f;
        [Min(0f)]
        public float absMinSpeed = 3f;
        [Range(0.1f, 20f)]
        public float releaseRate = 5f;
        [Range(0.1f, 20f)]
        public float applyRate = 3f;
        public bool useABS = true;

        [Header("Baked (Editor 焼き付けが代入・実行時は読出し専用)")]
        // 焼き付け時の body.mass。実行時の質量と違えば再焼き付けが必要。
        public float bakedBodyMass;
        public float bakedK0;
        public float bakedKneeX;
        public float bakedKEnd;
        public float bakedCCompress;
        public float bakedCRebound;
        public float bakedFzCap;
        public float bakedAlphaP;
        public float bakedInvR = 2f;
        public float bakedInvKappaPeak = 8.333333f;
        public float bakedInvSlideSpan = 1.1363636f;
        public float bakedInvAlphaP = 5.729578f;
        public float bakedInvMaxStroke = 2f;
        public float bakedMuSpan = -0.3f;
        public float bakedMuCap = 0.9f;
        public float bakedKc = 0.5f;

        [Header("Telemetry (read-only)")]
        public float outOmega;
        public float outBrakeEff;
        public float outStroke;
        public float outFz;
        public float outFx;
        public float outFy;
        public float outSlip;
        public float outAlphaDeg;
        public bool outGrounded;
        public bool outAbsRelease;
        public float outVelocityY;

        float _omega;
        float _bEff;
        bool _absReleasing;
        float _steerDeg;
        float _brakeCmd;
        float _spinTotal;
        float _k0;
        float _kneeX;
        float _kEnd;
        float _cComp;
        float _cReb;
        float _alphaP;

        float _strokeX;
        float _strokeDot;
        float _vx;
        float _vy;
        float _fz;
        float _fx;
        float _fy;
        float _kappa;
        float _alpha;
        Vector3 _axisA = Vector3.up;
        Vector3 _contactPoint = Vector3.zero;
        Vector3 _contactNormal = Vector3.up;
        Vector3 _forwardDir = Vector3.forward;
        Vector3 _lateralDir = Vector3.right;

        float _invR = 2f;
        float _invKappaPeak = 8.333333f;
        float _invSlideSpan = 1.1363636f;
        float _invAlphaP = 5.729578f;
        float _invMaxStroke = 2f;
        float _muSpan = -0.3f;
        float _muCap = 0.9f;
        float _fzCap = 1e9f;
        float _kcClamped = 0.5f;
        int _mask;

        void Start()
        {
            if (body != null)
            {
                Vector3 it = body.inertiaTensor;
                float iMax = it.x;
                if (it.y > iMax)
                    iMax = it.y;
                if (it.z > iMax)
                    iMax = it.z;
                if (iMax < body.mass * 0.25f)
                    Debug.LogError("[FDMiWheelCollider] Rigidbody inertia tensor far too small for its mass (radius of gyration < 0.5 m): any off-center gear force explodes the body. Disable Implicit Tensor and set a realistic tensor of order m*L^2/12 per axis.");
            }

            if (IsBakedUsable())
                LoadBaked();
            else
                FallbackRecompute();

            _mask = runwayMask.value;

            _omega = 0f;
            _bEff = 0f;
            _absReleasing = false;
            _spinTotal = 0f;
        }

        bool IsBakedUsable()
        {
            if (bakedBodyMass <= 0f)
                return false;
            if (bakedK0 <= 0f || bakedKEnd <= 0f)
                return false;
            if (bakedInvR <= 0f || bakedInvMaxStroke <= 0f)
                return false;
            return true;
        }

        void LoadBaked()
        {
            _k0 = bakedK0;
            _kneeX = bakedKneeX;
            _kEnd = bakedKEnd;
            _cComp = bakedCCompress;
            _cReb = bakedCRebound;
            _fzCap = bakedFzCap;
            _alphaP = bakedAlphaP;
            _invR = bakedInvR;
            _invKappaPeak = bakedInvKappaPeak;
            _invSlideSpan = bakedInvSlideSpan;
            _invAlphaP = bakedInvAlphaP;
            _invMaxStroke = bakedInvMaxStroke;
            _muSpan = bakedMuSpan;
            _muCap = bakedMuCap;
            _kcClamped = bakedKc;
        }

        // 未焼き付けの旧シーン救済。Editor の Bake と同式を Udon 上で再計算する (Start 1回のみ・Pow なし)。
        void FallbackRecompute()
        {
            float muS = muSlide;
            if (muS > muPeak)
                muS = muPeak;

            _alphaP = Mathf.Max(alphaPeakDeg, 0.5f) * Mathf.Deg2Rad;

            float mass = body != null ? body.mass : 200000f;
            if (mass < 1f)
                mass = 1f;
            float wi = Mathf.Max(loadShare, 0.001f);
            float target = mass * 9.81f * wi;
            _fzCap = maxLoadFactor * target;

            float xs = staticStroke;
            if (xs < 0.01f)
                xs = 0.01f;
            else if (xs > maxStroke * 0.95f)
                xs = maxStroke * 0.95f;
            _k0 = target / xs;

            _kneeX = kneeFraction * maxStroke;
            if (_kneeX < xs)
                _kneeX = xs;
            float erf = endRateFactor;
            if (erf < 1f)
                erf = 1f;
            _kEnd = _k0 * erf;

            float cCrit = 2f * Mathf.Sqrt(_k0 * mass * wi);
            float zc = zetaCompress;
            if (zc < 0f)
                zc = 0f;
            float zr = zetaRebound;
            if (zr < 0f)
                zr = 0f;
            _cComp = zc * cCrit;
            _cReb = zr * cCrit;

            _invR = 1f / tireRadius;
            float kp = Mathf.Max(kappaPeak, 1e-4f);
            _invKappaPeak = 1f / kp;
            _invSlideSpan = 1f / Mathf.Max(1f - kappaPeak, 1e-4f);
            _invAlphaP = 1f / _alphaP;
            _invMaxStroke = 1f / maxStroke;
            _muSpan = muS - muPeak;
            _muCap = muPeak >= muLateral ? muPeak : muLateral;
            _kcClamped = kappaCoupling < 0f ? 0f : (kappaCoupling > 1f ? 1f : kappaCoupling);
        }

        void FixedUpdate()
        {
            if (body == null)
                return;
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
                return;

            float steerCmd = 0f;
            float brakeCmd = 0f;
            if (steerInput != null)
                steerCmd = steerInput.Data;
            if (brakeInput != null)
                brakeCmd = brakeInput.Data;
            _steerDeg = Mathf.Clamp(steerCmd, -Mathf.Abs(steerLimitDeg), Mathf.Abs(steerLimitDeg));
            _brakeCmd = Mathf.Clamp01(brakeCmd);

            bool grounded = SampleContact();
            if (!grounded)
            {
                _fz = 0f;
                _fx = 0f;
                _fy = 0f;
                _kappa = 0f;
                _alpha = 0f;
                _absReleasing = false;
                _bEff = 0f;
                _spinTotal += _omega * dt;
                WrapSpin();
                PublishTelemetry(false);
                return;
            }

            ComputeStrutForce();
            UpdateABS(dt);
            UpdateWheelSpin(dt);
            _spinTotal += _omega * dt;
            WrapSpin();
            ComputeSlipAndTireForce();
            ApplyBodyForce();
            PublishTelemetry(true);
        }

        void WrapSpin()
        {
            if (_spinTotal >= 6.2831853f)
                _spinTotal -= 6.2831853f;
            else if (_spinTotal < 0f)
                _spinTotal += 6.2831853f;
        }

        bool SampleContact()
        {
            Vector3 o = transform.position;
            Vector3 a = transform.up;
            _axisA = a;
            float rayLen = maxStroke + tireRadius;
            RaycastHit hit;
            bool isHit = Physics.Raycast(o, -a, out hit, rayLen, _mask, QueryTriggerInteraction.Ignore);
            if (!isHit)
            {
                return false;
            }

            Vector3 p = hit.point;
            Vector3 n = hit.normal;
            float x = Mathf.Clamp(rayLen - hit.distance, 0f, maxStroke);

            Vector3 f0 = transform.forward;
            Vector3 fSteered = Quaternion.AngleAxis(_steerDeg, a) * f0;
            Vector3 f = fSteered - n * Vector3.Dot(fSteered, n);
            if (f.sqrMagnitude < 1e-8f)
                f = fSteered;
            else
                f = f.normalized;
            Vector3 t = Vector3.Cross(n, f);

            Vector3 v = body.GetPointVelocity(p);
            float vx = Vector3.Dot(v, f);
            float vy = Vector3.Dot(v, t);
            float vn = Vector3.Dot(v, n);
            float aDotN = Vector3.Dot(a, n);
            float xDot = 0f;
            if (aDotN > 0.1f)
                xDot = -vn / aDotN;

            _contactPoint = p;
            _contactNormal = n;
            _forwardDir = f;
            _lateralDir = t;
            _strokeX = x;
            _strokeDot = xDot;
            _vx = vx;
            _vy = vy;
            return true;
        }

        void ComputeStrutForce()
        {
            float fspring;
            if (_strokeX <= _kneeX)
                fspring = _k0 * _strokeX;
            else
                fspring = _k0 * _kneeX + _kEnd * (_strokeX - _kneeX);
            float xd = _strokeDot;
            if (xd > dampSpeedMax)
                xd = dampSpeedMax;
            else if (xd < -dampSpeedMax)
                xd = -dampSpeedMax;
            float c = xd >= 0f ? _cComp : _cReb;
            float fs = fspring + c * xd;
            if (fs > _fzCap)
                fs = _fzCap;
            if (fs < 0f)
                fs = 0f;
            float aDotN = Vector3.Dot(_axisA, _contactNormal);
            if (aDotN < 0f)
                aDotN = 0f;
            _fz = fs * aDotN;
        }

        void ComputeSlipAndTireForce()
        {
            float rOmega = tireRadius * _omega;
            float avx = Mathf.Abs(_vx);
            float arO = Mathf.Abs(rOmega);
            float denom = avx;
            if (arO > denom)
                denom = arO;
            if (denom < slipSpeedScale)
                denom = slipSpeedScale;
            float kappa = (rOmega - _vx) / denom;
            if (kappa > 1f)
                kappa = 1f;
            else if (kappa < -1f)
                kappa = -1f;
            _kappa = kappa;
            float ak = kappa >= 0f ? kappa : -kappa;

            float mux;
            if (ak <= kappaPeak)
                mux = muPeak * ak * _invKappaPeak;
            else
                mux = muPeak + _muSpan * (ak - kappaPeak) * _invSlideSpan;
            float sgnK = kappa > 0f ? 1f : (kappa < 0f ? -1f : 0f);
            float fxStar = _fz * sgnK * mux;

            float axDen = avx >= slipSpeedScale ? avx : slipSpeedScale;
            float lat = _vy / axDen * _invAlphaP;
            if (lat > 1f)
                lat = 1f;
            else if (lat < -1f)
                lat = -1f;
            float fyStar = -_fz * muLateral * lat * (1f - _kcClamped * ak);
            _alpha = lat * _alphaP;

            float aFx = fxStar >= 0f ? fxStar : -fxStar;
            float aFy = fyStar >= 0f ? fyStar : -fyStar;
            float cap = _muCap * _fz;
            float sum = aFx + aFy;
            float h = 1f;
            if (sum > cap && sum > 1e-6f)
                h = cap / sum;
            _fx = h * fxStar;
            _fy = h * fyStar;
        }

        void UpdateABS(float dt)
        {
            float b = _brakeCmd;
            float avx = Mathf.Abs(_vx);
            if (!(b > 0f && _fz > 0f && avx > absMinSpeed) || !useABS)
            {
                _absReleasing = false;
                _bEff = b;
                return;
            }

            float denom = avx >= slipSpeedScale ? avx : slipSpeedScale;
            float sgnVx = _vx >= 0f ? 1f : -1f;
            float sb = (avx - tireRadius * _omega * sgnVx) / denom;
            if (sb < 0f)
                sb = 0f;
            else if (sb > 1f)
                sb = 1f;
            if (_absReleasing)
            {
                if (sb < absLowSlip)
                    _absReleasing = false;
            }
            else
            {
                if (sb > absHighSlip)
                    _absReleasing = true;
            }

            if (_absReleasing)
                _bEff = Mathf.Min(b, Mathf.Max(0f, _bEff - releaseRate * dt));
            else
                _bEff = Mathf.Min(b, _bEff + applyRate * dt);
        }

        void UpdateWheelSpin(float dt)
        {
            float bC = _bEff;
            if (bC < 0f)
                bC = 0f;
            else if (bC > 1f)
                bC = 1f;
            float omegaTgt = _vx * (1f - bC) * _invR;
            float k = spinFollowRate * dt;
            if (k > 1f)
                k = 1f;
            else if (k < 0f)
                k = 0f;
            _omega += (omegaTgt - _omega) * k;
        }

        void ApplyBodyForce()
        {
            Vector3 f = _contactNormal * _fz + _forwardDir * _fx + _lateralDir * _fy;
            body.AddForceAtPosition(f, _contactPoint, ForceMode.Force);
        }

        void PublishTelemetry(bool grounded)
        {
            outOmega = _omega;
            outBrakeEff = _bEff;
            outStroke = _strokeX;
            outFz = _fz;
            outFx = _fx;
            outFy = _fy;
            outSlip = _kappa;
            outAlphaDeg = _alpha * Mathf.Rad2Deg;
            outGrounded = grounded;
            outAbsRelease = _absReleasing;
            outVelocityY = body.velocity.y;

            // 外部の可視化駆動用。縮みは正規化 0..1 (% の *100 分だけ安い 1 掛け算)、回転はラジアン累積。
            float norm = grounded ? _strokeX * _invMaxStroke : 0f;
            if (norm < 0f)
                norm = 0f;
            else if (norm > 1f)
                norm = 1f;
            if (suspensionOutput != null)
                suspensionOutput.Set(norm);
            if (spinOutput != null)
                spinOutput.Set(_spinTotal);
        }
    }
}
