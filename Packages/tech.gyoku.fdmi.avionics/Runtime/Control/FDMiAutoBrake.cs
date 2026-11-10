using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // オートブレーキの操舵則。モードで選んだ減速度を目標に、実減速度との差を積分して
    // ブレーキ指令 [0,1] を作る。出力は FDMiFloat だけ。
    //
    // Why not 速度そのものを目標にする: 停止位置は滑走路の状態で変わり、則が持つべき情報ではない。
    // 減速度を閉ループにすれば、摩擦や推力の違いをブレーキ量が吸収する。
    // Why not 車輪のブレーキ率を直接書く: どの輪を制動するかはデータの所属で決まる。
    // ~/Avionics/autobrake を輪側のブレーキ効果に配線すれば、輪の構成が変わってもここは動かない。
    // Why not 実減速度を目標にそのまま追わせる: 生の差分は 1 フレームの速度変化に敏感で、
    // 目標に当たった瞬間にブレーキが振動する。時定数 0.5s で落としてから比較する。
    [DefaultExecutionOrder(-300)]
    public class FDMiAutoBrake : FDMiBehaviour
    {
        [Header("入力")]
        [Tooltip("オートブレーキモードのパス")]
        public string autoBrakeModePath = "~/Input/AutoBrake/Value";

        [FDMiDataPath(nameof(autoBrakeModePath))]
        [Tooltip("モード。0=RTO, 1=OFF, 2=ONE, 3=TWO, 4=THREE, 5=MAX")]
        public FDMiFloat autoBrakeMode;

        [Tooltip("スロットルレバーのパス")]
        public string throttlePath = "~/Input/ThrottleLever/Throttle";

        [FDMiDataPath(nameof(throttlePath))]
        [Tooltip("スロットル [0,1]。RTO のラッチと作動条件に使う")]
        public FDMiFloat throttle;

        [Tooltip("接地状態のパス")]
        public string groundedPath = "~/Physics/IsGrounded";

        [FDMiDataPath(nameof(groundedPath))]
        [Tooltip("接地しているか。空中では作動しない")]
        public FDMiBool grounded;

        [Tooltip("左ヨークのブレーキ入力のパス")]
        public string yokeBrakeLPath = "~/Input/YokeL/BrakeInput";

        [FDMiDataPath(nameof(yokeBrakeLPath))]
        [Tooltip("ヨークのブレーキ入力（左）[0,1]。手動解除の判定に使う")]
        public FDMiFloat yokeBrakeL;

        [Tooltip("右ヨークのブレーキ入力のパス")]
        public string yokeBrakeRPath = "~/Input/YokeR/BrakeInput";

        [FDMiDataPath(nameof(yokeBrakeRPath))]
        [Tooltip("ヨークのブレーキ入力（右）[0,1]")]
        public FDMiFloat yokeBrakeR;

        [Header("出力")]
        [Tooltip("ブレーキ指令のパス")]
        public string brakeOutPath = "~/Avionics/autobrake";

        [FDMiDataPath(nameof(brakeOutPath))]
        [Tooltip("ブレーキ指令 [0,1] を書く FDMiFloat")]
        public FDMiFloat brakeOut;

        [Header("則")]
        [Tooltip("実減速度をならす時定数 [s]")]
        [Min(0.01f)]
        public float decelTau = 0.5f;

        [Tooltip("減速度の偏差→ブレーキ指令のゲイン [1/(m/s^2*s)]")]
        public float brakeGain = 0.5f;

        [Tooltip("物体。速度と接地判定の基準")]
        public Rigidbody body;

        float[] _mode;
        float[] _throttle;
        bool[] _grounded;
        float[] _yokeBrakeL;
        float[] _yokeBrakeR;
        float[] _brakeOut;

        float _brake;       // 積分状態。これがそのまま出力になる
        float _decel;       // ならした実減速度 [m/s^2]
        float _prevGs;      // 前フレームの対地速度 [m/s]
        int _modePrev;
        bool _latched;      // RTO のラッチ
        bool _disarmed;     // 手動ブレーキでの解除。モード変更か接地解除まで保持する
        bool _wasActive;
        float _activeTime;  // 作動開始からの経過 [s]

        void Start()
        {
            _mode = autoBrakeMode.data;
            _throttle = throttle.data;
            _grounded = grounded.data;
            _yokeBrakeL = yokeBrakeL.data;
            _yokeBrakeR = yokeBrakeR.data;
            _brakeOut = brakeOut.data;

            _modePrev = (int)_mode[0];

            Vector3 v = body.velocity;
            _prevGs = new Vector3(v.x, 0f, v.z).magnitude;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // モード変更は解除も兼ねる。モードを切り替えたのに前のモードのブレーキ量が
            // 残ると、意図しない制動が続く。
            int mode = (int)_mode[0];
            if (mode != _modePrev)
            {
                _modePrev = mode;
                _brake = 0f;
                _latched = false;
                _disarmed = false;
                _activeTime = 0f;
                _wasActive = false;
            }

            // 水平成分だけを対地速度とする。降下や上昇は車輪の減速と関係ない。
            Vector3 v = body.velocity;
            float gs = new Vector3(v.x, 0f, v.z).magnitude;

            // 減速度は正を「減速」とする。生の差分は 1 フレームで大きく跳ぶため、
            // 1 次遅れでならしてから則に渡す。
            float rawDecel = -(gs - _prevGs) / dt;
            _prevGs = gs;
            float a = dt / (decelTau + dt);
            _decel += a * (rawDecel - _decel);

            bool isGrounded = _grounded[0];
            bool active = false;

            if (!isGrounded || mode == 1)
            {
                // 空中と OFF では作動しない。ラッチも解除も持ち越さない。
                _brake = 0f;
                _latched = false;
                _disarmed = false;
                _activeTime = 0f;
                _wasActive = false;
            }
            else if (mode == 0)
            {
                // RTO は離陸滑走のため、スロットルを入れた時点で武装し、
                // スロットルを戻してから作動する。武装の判定を作動中に行うと、
                // スロットルを戻した瞬間に武装できず永久に動かなくなる。
                if (_throttle[0] > 0.5f)
                    _latched = true;
                active = _latched && _throttle[0] < 0.02f && !_disarmed;
            }
            else
            {
                active = !_disarmed;
            }

            if (active)
            {
                if (!_wasActive)
                    _activeTime = 0f;
                _activeTime += dt;

                // 手でブレーキを踏んだら降りる。作動直後の 3 秒は判定しないのは、
                // ヨークの入力が 0 に戻りきる前の残りで解除されるのを防ぐため。
                if (_yokeBrakeL[0] + _yokeBrakeR[0] > 0.1f && _activeTime > 3f)
                {
                    _disarmed = true;
                    _brake = 0f;
                    active = false;
                }
            }
            else
            {
                _activeTime = 0f;
            }
            _wasActive = active;

            if (!active)
            {
                _brake = 0f;
            }
            else if (gs >= 15.43f)
            {
                // 目標減速度 [m/s^2]。フィート毎秒毎秒で定義された実機の値を換算する。
                float target = 0f;
                if (mode == 0)
                {
                    target = 14f * 0.3048f;
                }
                else if (mode == 2)
                {
                    target = 4f * 0.3048f;
                }
                else if (mode == 3)
                {
                    target = 5f * 0.3048f;
                }
                else if (mode == 4)
                {
                    target = 7.2f * 0.3048f;
                }
                else if (mode == 5)
                {
                    // MAX は低速域だけ 12 ft/s^2 に落とす。14 のままだと停止間際で
                    // ブレーキが効き過ぎて機首が跳ねる。
                    if (gs < 41.15f)
                        target = 12f * 0.3048f;
                    else
                        target = 14f * 0.3048f;
                }

                _brake = Mathf.Clamp01(_brake + brakeGain * (target - _decel) * dt);
            }
            // 30kt 未満では積分しない。停止間際の減速度はノイズが大きく、
            // 追わせるとブレーキがばたつく。直前の値を保持して出力する。

            _brakeOut[0] = _brake;
        }
    }
}
