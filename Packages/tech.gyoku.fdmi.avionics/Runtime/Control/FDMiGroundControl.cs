using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 地上操舵とブレーキの操舵則。ラダー入力を操向角に、ブレーキ入力を制動率に変え、
    // FDMiFloat の配列にだけ書く。FDMiWheelCollider は直接参照しない。
    //
    // 操向する輪・制動する輪はデータの所属で決まる。操向角は ~/Wheel/Steer/**/Value、
    // 制動率は ~/Wheel/Brake/**/Value に置き、そこに入れた輪だけが動く。
    // Why not FDMiWheelCollider[]: コンポーネント間の参照を切る。FDMiFloat 経由なら
    // 車輪の実装 (WheelCollider か否か) に依存せず、可視化も同じデータを読める。
    // Why not 輪ごとの最大操向角: 機械的な上限は車輪の steerLimitDeg が持つ。ここに二重に
    // 持つとシーン側で値が食い違う。輪ごとの差は「どの輪を steerOut に入れるか」で表す。
    // Why not レート制限: ラダー入力は手の操作で既に滑らかで、固定ステップごとに現在の
    // 入力へ追従させれば足りる。ノーズギア操舵のレートを則側で落とす必要はない。
    [DefaultExecutionOrder(-200)]
    public class FDMiGroundControl : FDMiBehaviour
    {
        [Header("入力")]
        [Tooltip("パーキングブレーキのパス")]
        public string parkBrakePath = "~/Input/ParkBrakeLever/Value";

        [FDMiDataPath(nameof(parkBrakePath))]
        [Tooltip("パーキングブレーキ [0,1]")]
        public FDMiFloat parkBrake;

        [Tooltip("左ヨークのブレーキ入力パス")]
        public string brakeLPath = "~/Input/YokeL/BrakeInput";

        [FDMiDataPath(nameof(brakeLPath))]
        [Tooltip("ヨー入力（左）[-1,1]")]
        public FDMiFloat brakeL;

        [Tooltip("右ヨークのブレーキ入力パス")]
        public string brakeRPath = "~/Input/YokeR/BrakeInput";

        [FDMiDataPath(nameof(brakeRPath))]
        [Tooltip("ヨー入力（右）[-1,1]")]
        public FDMiFloat brakeR;

        [Tooltip("左ヨークのヨー入力パス")]
        public string yawLPath = "~/Input/YokeL/YawInput";

        [FDMiDataPath(nameof(yawLPath))]
        [Tooltip("ヨー入力（左）[-1,1]")]
        public FDMiFloat yawL;

        [Tooltip("右ヨークのヨー入力パス")]
        public string yawRPath = "~/Input/YokeR/YawInput";

        [FDMiDataPath(nameof(yawRPath))]
        [Tooltip("ヨー入力（右）[-1,1]")]
        public FDMiFloat yawR;

        [Header("出力")]
        [Tooltip("操向角のパス。操向する輪の FDMiFloat をまとめる")]
        public string steerPath = "~/Physics/Wheel/Steer";

        [FDMiDataPath(nameof(steerPath))]
        [Tooltip("操向角 [deg] を書く FDMiFloat。操向する輪だけを置く")]
        public FDMiFloat steerOut;

        [Tooltip("制動率のパス。制動する輪の FDMiFloat をまとめる")]
        public string brakeOutPath = "~/Physics/Wheel/**/Brake";

        [FDMiDataPath(nameof(brakeOutPath))]
        [Tooltip("制動率 [0,1] を書く FDMiFloat。制動する輪だけを置く")]
        public FDMiFloat[] brakeOut;

        [Header("操舵則")]
        [Tooltip("最大操向角 [deg]。車輪側の steerLimitDeg でさらにクランプされる")]
        [Range(0f, 90f)]
        public float steerMaxDeg = 70f;

        [Header("ブレーキ")]
        [Tooltip("Yokeのブレーキ量→ブレーキの指令")]
        [Range(0f, 1f)]
        public float[] brakeEffect;

        [Tooltip("ラダー操舵量→ブレーキの指令")]
        [Range(-1f, 1f)]
        public float[] steerBrakeEffect;

        [Tooltip("パーキングブレーキ→ブレーキの指令")]
        [Range(0f, 1f)]
        public float[] parkBrakeEffect;

        float[] _parkBrake;
        float[] _brakeL;
        float[] _brakeR;
        float[] _yawL;
        float[] _yawR;
        float[] _steerOut;

        void Start()
        {
            _brakeL = brakeL.data;
            _brakeR = brakeR.data;
            _parkBrake = parkBrake.data;
            _yawL = yawL.data;
            _yawR = yawR.data;
            _steerOut = steerOut.data;
        }

        void FixedUpdate()
        {
            float yaw = Mathf.Clamp(_yawL[0] + _yawR[0], -1, 1);
            _steerOut[0] = yaw * steerMaxDeg;

            float brake = _brakeL[0] + _brakeR[0];
            for (int i = 0; i < brakeOut.Length; i++)
            {
                float eff =
                    brake * brakeEffect[i]
                    + Mathf.Clamp01(yaw * steerBrakeEffect[i])
                    + _parkBrake[0] * parkBrakeEffect[i];
                brakeOut[i].data[0] = Mathf.Clamp01(eff);
            }
        }
    }
}
