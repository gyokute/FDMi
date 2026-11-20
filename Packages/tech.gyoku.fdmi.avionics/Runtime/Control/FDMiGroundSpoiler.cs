using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 地上スポイラ。武装中だけスポイラレバーを接地で最大、空中で 0 へ動かす。
    //
    // Why not 面の舵角を直接書く: 地上スポイラはスポイラレバー > 1 のデテントで表し、
    // レバーから舵角への変換は FDMiFlightControl が持つ。ここはレバーだけを動かせばよい。
    // Why not 最大到達で自分を無効化する: レバーが 1.2 に残ったまま離陸し、空中で
    // 0 へ戻せない。最大では書き込みを止めるだけにし、接地が外れたら再開して 0 へ戻す。
    // Why not 最大で止めたままにする: スポイラレバーは同期チャネルでもある。古い同期値が
    // 最大を上書きすると、止まったままでは再展開できない。最大から動いたらラッチを
    // 解除して追従し直す。
    // Why not 武装解除時に 0 を書く: 武装は機能の有効化であり、解除後はパイロットが
    // 手動でレバーを動かせるべきだから。武装 false では一切書かない。
    [DefaultExecutionOrder(-250)]
    public class FDMiGroundSpoiler : FDMiBehaviour
    {
        [Header("入力")]
        [Tooltip("地上スポイラ武装のパス")]
        public string groundSpoilerPath = "~/Input/**/GroundSpoiler";

        [FDMiDataPath(nameof(groundSpoilerPath))]
        [Tooltip("True の間だけ地上スポイラを駆動する")]
        public FDMiBool groundSpoiler;

        [Tooltip("接地状態のパス")]
        public string groundedPath = "~/Physics/IsGrounded";

        [FDMiDataPath(nameof(groundedPath))]
        [Tooltip("接地しているか")]
        public FDMiBool grounded;

        [Header("出力")]
        [Tooltip("スポイラレバーのパス")]
        public string spoilerLeverPath = "~/Input/SpoilerLever/Value";

        [FDMiDataPath(nameof(spoilerLeverPath))]
        [Tooltip("スポイラレバー [0,1.2] を書く FDMiFloat")]
        public FDMiFloat spoilerLever;

        [Header("則")]
        [Tooltip("接地時に動かすレバー位置。1 より上で地上スポイラのデテント")]
        [Min(0f)]
        public float maxLever = 1.2f;

        [Tooltip("レバーの作動速度 [1/s]")]
        [Min(0.01f)]
        public float leverRate = 0.6f;

        bool[] _groundSpoiler;
        bool[] _grounded;
        float[] _spoilerLever;
        bool _latched;

        void Start()
        {
            _groundSpoiler = groundSpoiler.data;
            _grounded = grounded.data;
            _spoilerLever = spoilerLever.data;
        }

        void FixedUpdate()
        {
            if (!_groundSpoiler[0])
            {
                _latched = false;
                return;
            }

            if (_latched)
            {
                if (_grounded[0] && Mathf.Approximately(_spoilerLever[0], maxLever))
                    return;
                _latched = false;
            }

            float target = _grounded[0] ? maxLever : 0f;
            spoilerLever.Data = Mathf.MoveTowards(
                _spoilerLever[0],
                target,
                leverRate * Time.fixedDeltaTime
            );

            if (_grounded[0] && Mathf.Approximately(_spoilerLever[0], maxLever))
                _latched = true;
        }
    }
}
