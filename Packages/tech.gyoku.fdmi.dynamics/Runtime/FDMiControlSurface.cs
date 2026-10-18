using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 操舵面1枚分の舵角効果。V1 (tag 1.0.0 の FDMi_ControlSurface) の移植。
    // 舵角を区間ごとの配分比で配り、区間ごとの迎え角・係数・面積比の加算量をカーブから引く。
    // 区間数と並びは FDMiWing の区間配列に合わせる。配列の長さはインスペクタで与える。
    //
    // V1 の actuatorControl を FDMiFloat 入力に置き換えた。舵の遅れは入力側のモジュールで作る。
    // ここは与えられた舵角を引くだけにする。
    public class FDMiControlSurface : FDMiBehaviour
    {
        [Header("配線")]
        // 舵角。度。正は後縁下げ。V1 の actuatorControl.val に相当。
        public FDMiFloat input;

        [Header("設計値")]
        // 区間ごとの舵角の配分比。FDMiWing の区間数と同じ長さにする。
        public float[] surfaceMul;

        [Header("カーブ")]
        // 舵角 [deg] に対する迎え角の加算量。
        public AnimationCurve alphaDeg;
        // 舵角 [deg] に対する揚力係数の加算量。
        public AnimationCurve clDeg;
        // 舵角 [deg] に対する抗力係数の加算量。
        public AnimationCurve cdDeg;
        // 舵角 [deg] に対するピッチングモーメント係数の加算量。
        public AnimationCurve cmDeg;
        // 舵角 [deg] に対する面積比。舵を張り出せば 1 より大きくなる。
        public AnimationCurve areaDeg;

        [Header("区間ごとの出力")]
        // FDMiWing が読む。surfaceMul と同じ長さにする。
        public float[] alpha;
        public float[] cl;
        public float[] cd;
        public float[] cm;
        public float[] area;

        void FixedUpdate()
        {
            float deflection = 0f;
            if (input != null)
                deflection = input.Data;
            for (int i = 0; i < surfaceMul.Length; i++)
            {
                float deg = deflection * surfaceMul[i];
                alpha[i] = alphaDeg.Evaluate(deg);
                cl[i] = clDeg.Evaluate(deg);
                cd[i] = cdDeg.Evaluate(deg);
                cm[i] = cmDeg.Evaluate(deg);
                area[i] = areaDeg.Evaluate(deg);
            }
        }
    }
}
