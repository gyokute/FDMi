using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public enum FDMiAxisBehaviourType
    {
        Momentum,
        Alternate,
        Force,
        Addition,
    }

    public class FDMiHandAxisInput : FDMiHandInputElement
    {
        [Tooltip("出力データの解決パス。名前空間の区切りは /")]
        public string outputPath = "Value";

        [FDMiDataPath(nameof(outputPath))]
        [Tooltip("操作量を書き込む出力。outputPath から解決する")]
        public FDMiData output;

        [Tooltip("読む入力軸")]
        public FDMiHandAxisType inputAxisType = FDMiHandAxisType.Trigger;

        [Tooltip("軸値を出力へ変換する規則")]
        public FDMiAxisBehaviourType behaviourType;

        [Tooltip("軸値の倍率")]
        public float multiply = 1f;

        [Tooltip("出力の下限")]
        public float min;

        [Tooltip("出力の上限")]
        public float max = 1f;

        [Tooltip("グラブ終了時に戻す値")]
        public float initial;

        [Tooltip("Force と Alternate の判定しきい値")]
        public float threshold = 0.5f;
        private bool alternateLatch;

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            float input = group.axes[(int)inputAxisType] * multiply;
            if (behaviourType == FDMiAxisBehaviourType.Momentum)
            {
                output.Set(Mathf.Clamp(input, min, max));
            }
            else if (behaviourType == FDMiAxisBehaviourType.Force)
            {
                if (input > threshold)
                    output.Set(max);
            }
            else if (behaviourType == FDMiAxisBehaviourType.Alternate)
            {
                if (input > threshold)
                {
                    if (!alternateLatch)
                    {
                        output.Set(Mathf.Approximately(max, output.GetFloat()) ? min : max);
                        alternateLatch = true;
                    }
                }
                else
                    alternateLatch = false;
            }
            else if (behaviourType == FDMiAxisBehaviourType.Addition)
            {
                output.Set(Mathf.Clamp(output.GetFloat() + input * Time.deltaTime, min, max));
            }
        }

        public override void OnGrabEnd()
        {
            alternateLatch = false;
            if (behaviourType == FDMiAxisBehaviourType.Momentum)
                output.Set(initial);
        }
    }
}
