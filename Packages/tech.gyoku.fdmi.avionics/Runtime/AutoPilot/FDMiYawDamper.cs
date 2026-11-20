using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ヨーダンパの操舵則。機体軸のヨーレートをハイパスして、逆向きのラダー量を作る。
    // 出力は FDMiFloat だけ。
    //
    // Why not ヨーレートそのものを戻す: 定常旋回でも舵が当たり続け、旋回が浅くなる。
    // 操縦者の意図した旋回はゆっくりした成分なので、時定数 2s のウォッシュアウトで
    // 落として、短周期の揺れだけを抑える。
    // Why not スイッチを持つ: 実機のヨーダンパは常時作動で、切ると操縦が急に敏感になる。
    // ここでも常時作動にして、機体側の設定でゲインを絞る。
    [DefaultExecutionOrder(-300)]
    public class FDMiYawDamper : FDMiBehaviour
    {
        [Header("入力")]
        [Tooltip("物体。角速度の基準")]
        public Rigidbody body;

        [Header("出力")]
        [Tooltip("ラダー指令のパス")]
        public string rudderPath = "~/Input/YawDamper/Rudder";

        [FDMiDataPath(nameof(rudderPath))]
        [Tooltip("ラダー指令 [-1,1] を書く FDMiFloat")]
        public FDMiFloat rudder;

        [Header("則")]
        [Tooltip("ウォッシュアウトの時定数 [s]")]
        [Min(0.01f)]
        public float tau = 2f;

        [Tooltip("ヨーレート→ラダーのゲイン [1/(deg/s)]")]
        public float gain = 0.02f;

        [Tooltip("ラダー指令の上限 [0,1]")]
        [Range(0f, 1f)]
        public float authority = 0.2f;

        float[] _rudder;
        float _hp;
        float _prevYawRate;

        void Start()
        {
            _rudder = rudder.data;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 機体の上方向への射影をヨーレートとする。ワールド Y だと機体が傾いたときに
            // 旋回と関係ない成分が混ざる。
            float yawRate = Vector3.Dot(body.angularVelocity, body.transform.up) * Mathf.Rad2Deg;

            // 1 次ハイパス。積分器を 1 本持つだけで、直流成分 (定常旋回) は消える。
            float a = tau / (tau + dt);
            _hp = a * (_hp + yawRate - _prevYawRate);
            _prevYawRate = yawRate;

            // ヨーが正なら逆向きのラダーを当てる。機体のヨー軸の符号は
            // モデル依存なので、ゲインの符号で合わせられるようにしておく。
            _rudder[0] = Mathf.Clamp(-gain * _hp, -authority, authority);
        }
    }
}
