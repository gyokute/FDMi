using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 機体の質量・重心・慣性テンソルを Rigidbody に与える。値はすべて body ローカル (機体固定座標)。
    // +Z = 前方、+X = 右、+Y = 上。
    //
    // 質量特性そのものは Editor が一度だけ焼き付ける (FDMiBodyMassEditor / FDMiBodyMassBake)。
    // ランタイムが毎ステップ読むのは質量 [kg] と MAC重心率 [0..1] の2つだけで、どちらも FDMiFloat で受ける。
    // MAC重心率は主翼の MAC 前縁を 0.0、後縁を 1.0 とする。
    //
    // 前後方向の重心は MAC重心率から求める。左右・上下は質量配分から焼いた centerOfMass の x/y を使う。
    // MAC の関係 (前縁 z と率あたりの z 移動量) は Editor が焼き付ける。FixedUpdate で FDMiWing を
    // 読みに行かないのは、区間配列の走査と弦長・面積の再計算を毎ステップ払わないため。
    //
    // 慣性テンソルは焼き付け値で固定する。MAC重心率が変わっても再計算しない。
    // 重心移動が遅いことを前提に、毎ステップのテンソル再計算 (U# では重い) を避けるための割り切り。
    //
    // Rigidbody の自動重心・自動テンソルは切る。切らないと centerOfMass / inertiaTensor への
    // 代入が無視される (シーンの Rigidbody は CoM 自動が既定)。
    public class FDMiBodyMass : FDMiBehaviour
    {
        [Header("配線")]
        [Tooltip("質量特性を与える機体の Rigidbody。Editor が解決する")]
        public Rigidbody body;

        [Tooltip("質量の解決パス")]
        public string MassPath = "~/Physics/Mass";

        [FDMiDataPath(nameof(MassPath))]
        [Tooltip("機体質量 [kg]")]
        public FDMiFloat Mass;

        [Tooltip("MAC重心率の解決パス")]
        public string MacCGPath = "~/Physics/CG";

        [FDMiDataPath(nameof(MacCGPath))]
        [Tooltip("MAC重心率 [0..1]。0.0 = 主翼MAC前縁、1.0 = 主翼MAC後縁")]
        public FDMiFloat MacCG;

        [Header("設計値 (Editor 焼き付け)")]
        // 焼き付けの基準になる総質量。Unity の Rigidbody.mass は実行時に FDMiFloat で上書きされるため、
        // 設計質量は別に持つ (Editor で Inspector を開き直しても実行時の質量に引きずられない)。
        [Tooltip("設計質量 [kg]。Editor の焼き付け基準")]
        public float designMass;

        // 焼き付け結果。Inspector と Scene view は FDMiBodyMassEditor が表示する。
        // 形状から一度だけ求める値なので、ランタイムでは読み出すだけ。
        // centerOfMass は焼き付け時の合成重心 (x/y = 質量重心、z = 設計MAC重心率の点)。
        // 実行時の z は macZ0 + macSlope * MAC重心率 で置き換える。
        [HideInInspector] public Vector3 centerOfMass;
        [HideInInspector] public Vector3 inertiaTensor = Vector3.one;
        [HideInInspector] public float macZ0;
        [HideInInspector] public float macSlope;
        [HideInInspector] public float bakedMacFraction;
        [HideInInspector] public Vector3 macLeadingEdge;
        [HideInInspector] public Vector3 macDirection = Vector3.back;
        [HideInInspector] public float macChord;
        [HideInInspector] public FDMiWing mainWing;
        [HideInInspector] public float wingMass;
        [HideInInspector] public float fuselageMass;
        [HideInInspector] public float engineMass;

        // FDMiFloat の参照は Start で配列ごと受ける。毎ステップの Data プロパティ呼び出しを避ける
        // (FDMiWing / FDMiBodyAero と同じ理由)。
        float[] _mass;
        float[] _macCG;

        void Start()
        {
            _mass = Mass != null ? Mass.data : null;
            // MAC重心率は常に接続されている前提。未接続時のフォールバックは持たない。
            _macCG = MacCG.data;

            body.automaticCenterOfMass = false;
            body.automaticInertiaTensor = false;
            body.inertiaTensorRotation = Quaternion.identity;
            body.inertiaTensor = inertiaTensor;

            Apply();
        }

        void FixedUpdate()
        {
            Apply();
        }

        // 毎ステップの代入は質量と重心だけ。テンソルは Start で固定済み。
        void Apply()
        {
            body.mass = _mass != null ? _mass[0] : designMass;
            float fraction = _macCG[0];
            body.centerOfMass = new Vector3(centerOfMass.x, centerOfMass.y, macZ0 + macSlope * fraction);
        }
    }
}
