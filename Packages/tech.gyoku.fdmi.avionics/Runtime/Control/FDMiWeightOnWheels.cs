using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 接地判定。車輪のサス縮みを集約し、いずれかが縮んでいれば接地とする。
    //
    // Why not FDMiWheelCollider を直接参照する: 車輪の実装に依存せず、サス出力の
    // データノードだけで接地を作れる。輪の増減はパス解決に任せられる。
    // Why not レーダー高度: 2 m 以下を接地とすると、フレア中に接地扱いになり、
    // 地上スポイラが車輪の接地前に開いて揚力を殺す。サスの縮みは車輪が地面に
    // 触れたときだけ出る。
    // Why not 縮みの閾値を設ける: SuspOut は非接地で厳密に 0、接地で 0 以上を書く。
    // 0 より大きいことが接触の定義と一致する。沈み込みの深さは接地の有無と別。
    [DefaultExecutionOrder(-350)]
    public class FDMiWeightOnWheels : FDMiBehaviour
    {
        [Tooltip("サス出力のパス。接地判定に使う輪の FDMiFloat をまとめる")]
        public string suspensionPath = "~/Physics/Wheel/**/SuspOut";

        [FDMiDataPath(nameof(suspensionPath))]
        [Tooltip("サス縮み [0,1] を読む FDMiFloat。いずれかが 0 より大きければ接地")]
        public FDMiFloat[] suspension;

        [Tooltip("接地状態のパス")]
        public string groundedPath = "~/Physics/IsGrounded";

        [FDMiDataPath(nameof(groundedPath))]
        [Tooltip("接地しているかを書く FDMiBool")]
        public FDMiBool grounded;

        bool[] _grounded;

        void Start()
        {
            _grounded = grounded.data;
        }

        void FixedUpdate()
        {
            bool any = false;
            for (int i = 0; i < suspension.Length; i++)
            {
                if (suspension[i].data[0] > 0f)
                {
                    any = true;
                    break;
                }
            }
            _grounded[0] = any;
        }
    }
}
