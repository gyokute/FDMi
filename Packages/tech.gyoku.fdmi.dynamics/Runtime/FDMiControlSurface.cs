using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 装置の種類。効かせ方が違うので、ここで分ける。
    // 動翼は等価迎え角、フラップは揚力の加算、スラットは失速角の延長、スポイラは揚力の倍率で効かせる。
    public enum FDMiWingDeviceType
    {
        Control,    // 動翼。エルロン・エレベータ・ラダー。
        Flap,       // 後縁フラップ。スロッテド。
        Slat,       // 前縁スラット。
        Spoiler,    // エアブレーキ。上翼面スポイラ。
    }

    // 翼の装置1つ分の効果。V1 (tag 1.0.0 の FDMi_ControlSurface) のカーブ入力を、設計値から導く式に
    // 置き換えた。装置1つが1コンポーネントで、区間数と並びは FDMiWing の区間配列に合わせる。
    //
    // 動翼の効きは薄翼理論の舵効果 τ = 1 - (θ - sinθ)/π, θ = acos(2 c_e/c - 1)。
    // c_e/c=0.25 で τ=0.61 となり、DATCOM の plain flap の ΔCl_δ ≈ 0.06/deg と一致する。
    // フラップとスラットの揚力増分は DATCOM 流に ΔClmax をチャート値として与える。
    // スラットは失速角を ΔClmax / 2π だけ延長し、減ったぶんの揚力を加算で戻す形にする。
    // スポイラは平板の抗力 2 sin²δ と、コード比に比例する揚力の欠損で表す。
    //
    // 舵の遅れ (アクチュエータ) とヒンジモーメントは持たない。入力側のモジュールで作る。
    // Why not 焼き付け用の Editor: τ はコード比だけで決まるので、押す手間を増やすほどの量ではない。
    [DefaultExecutionOrder(-100)]
    public class FDMiControlSurface : FDMiBehaviour
    {
        [Header("配線")]
        // 舵角。度。正は後縁下げ(動翼・フラップ)・前縁突出(スラット)・開度(スポイラ)。
        public FDMiFloat input;

        [Header("装置")]
        // 装置の種類。効かせ方が変わる。
        public FDMiWingDeviceType type = FDMiWingDeviceType.Control;
        // 装置のコード比。動翼は舵の効き、フラップ・スラット・スポイラは抗力の基準。0 がなし、1 が全弦。
        [Range(0f, 1f)]
        public float chordRatio = 0.25f;
        // 最大舵角。度。効果の正規化の基準。
        [Min(1f)]
        public float maxDeflectionDeg = 50f;
        // 最大舵角での区間 (2次元) の揚力係数増分。DATCOM のセクションのチャート値。フラップとスラットが使う。
        // 機体の CLmax 増分は、これ × 装置が覆う面積比 × FDMiWing の outLiftGain になる。
        // 機体で ΔCLmax を +1.0 欲しければ、面積比 0.6・outLiftGain 0.8 として 2 前後を入れる。
        public float deltaClMax = 1f;

        [Header("区間")]
        // 区間ごとの配分比。FDMiWing の区間数と同じ長さにする。装置が覆う区間だけ 1 にする。
        public float[] sectionMul;

        [Header("区間ごとの出力")]
        // FDMiWing が読む。sectionMul と同じ長さにする。
        // 区間の等価迎え角シフト。度。
        public float[] alpha;
        // 区間の加算揚力係数。
        public float[] cl;
        // 区間の加算抗力係数。
        public float[] cd;
        // 区間の揚力倍率。スポイラだけ 1 未満になる。
        public float[] scale;

        // 2次元翼型の揚力傾斜。スラットの失速角延長を揚力係数の増分に換算する。
        const float _a2D = 6.2831853f;
        // 動翼とフラップの抗力係数。後縁の舵面が流れを曲げるぶんの目安。
        const float _cdDeflect = 0.8f;
        // スラットの抗力係数。前縁の出っ張りぶんだけ。
        const float _cdSlat = 0.05f;
        // スポイラの抗力係数。平板が流れに直交するときの値。
        const float _cdSpoiler = 2f;
        // スポイラの揚力損失係数。コード比と sinδ に掛かる。
        const float _liftLossSpoiler = 2.5f;

        // Start() で求める。舵効果と、その他の換算値。
        float _tau;
        float _invMaxDeflection;
        float _stallExtDeg;

        void Start()
        {
            // 薄翼理論の舵効果。コード比 0 で 0、全弦で 1 になる。
            float theta = Mathf.Acos(2f * chordRatio - 1f);
            _tau = 1f - (theta - Mathf.Sin(theta)) / Mathf.PI;
            _invMaxDeflection = 1f / maxDeflectionDeg;
            // 失速角の延長。ΔClmax を 2次元揚力傾斜で割った角度だけ延ばせば、低迎角の揚力を
            // 変えずに CLmax を ΔClmax だけ上げられる。
            _stallExtDeg = deltaClMax * Mathf.Rad2Deg / _a2D;
        }

        void FixedUpdate()
        {
            float deg = 0f;
            if (input != null)
                deg = input.Data;

            // 型ごとの係数を先に求める。区間ループには分岐を持ち込まない。
            float dRad = Mathf.Deg2Rad * deg;
            float alphaPer = 0f;
            float clPer = 0f;
            float cdPer = 0f;
            float lossPer = 0f;

            if (type == FDMiWingDeviceType.Control)
            {
                // 舵角を等価な迎え角に置き換える。揚力の増分は区間の曲線の傾斜が決める。
                alphaPer = _tau * deg;
                cdPer = _cdDeflect * chordRatio * dRad * dRad;
            }
            else if (type == FDMiWingDeviceType.Flap)
            {
                clPer = deltaClMax * deg * _invMaxDeflection;
                cdPer = _cdDeflect * chordRatio * dRad * dRad;
            }
            else if (type == FDMiWingDeviceType.Slat)
            {
                float frac = deg * _invMaxDeflection;
                // 失速角を延ばす向きに迎え角をずらし、減ったぶんの揚力を加算で戻す。
                alphaPer = -_stallExtDeg * frac;
                clPer = deltaClMax * frac;
                cdPer = _cdSlat * chordRatio * frac * frac;
            }
            else
            {
                float sin = Mathf.Sin(dRad);
                cdPer = _cdSpoiler * chordRatio * sin * sin;
                lossPer = _liftLossSpoiler * chordRatio * sin;
            }

            for (int i = 0; i < sectionMul.Length; i++)
            {
                float m = sectionMul[i];
                alpha[i] = alphaPer * m;
                cl[i] = clPer * m;
                cd[i] = cdPer * m;
                scale[i] = 1f - lossPer * m;
            }
        }
    }
}
