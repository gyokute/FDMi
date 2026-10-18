using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 翼の設計値から実行時係数を焼き付ける。Editor 側の通常 C# で動くため、
    // 三角関数・平方根・べき乗を自由に使える。Udon の制約は受けない。
    // ここで出す係数は形が変わらない限り変わらないので、実行時は掛け算だけ残る。
    // 式は DATCOM 流の近似を一枚翼・一点計算に縮退させたもので、厳密解ではない。
    // 調整はすべて実機基準で行うため、近似の置き方をコメントに残す。
    public static class FDMiWingBake
    {
        // 度とラジアンの換算。
        const float Deg = 0.0174532925f;

        // 設計値から焼き付け済み係数をすべて求めて翼に書き込む。
        public static void Bake(FDMiWing w)
        {
            // 平面形。台形翼の面積・テーパー比・平均空力弦・アスペクト比。
            float b = w.span;
            float cr = w.rootChord;
            float ct = w.tipChord;
            float taper = ct / cr;
            float area = 0.5f * (cr + ct) * b;
            float ar = b * b / area;

            // 揚力傾斜。ヘルムボルトの楕円翼式に後退角の余弦と胴体持上げを掛ける。
            // 後退角が大きい三角翼の渦揚力までは表せない。亜音速の直線翼・後退翼用と割り切る。
            float cosSweep = Mathf.Cos(w.sweepDeg25 * Deg);
            float helm = 2f * Mathf.PI * ar / (2f + Mathf.Sqrt(4f + ar * ar));
            float bodyGain = 1f + 0.3f * (w.bodyDiameter / b);
            w.bakedArea = area;
            w.bakedLiftSlope = helm * cosSweep * bodyGain;

            // 零揚力角。取付角は前縁上げを正とし、その分だけ零揚力角を下げる。
            w.bakedAlpha0Rad = (w.airfoilAlpha0Deg - w.incidenceDeg) * Deg;

            // 失速上限。後退角で少し下げる。下限はキャンバー翼の目安で上限の8割裏返し。
            w.bakedCLmax = w.airfoilClMax * cosSweep;
            w.bakedCLmin = -0.8f * w.bakedCLmax;

            // 誘導抗力係数。オズワルド係数は DATCOM 流の近似式で求める。
            float e = 1.78f * (1f - 0.045f * Mathf.Pow(ar, 0.68f)) - 0.64f;
            w.bakedK = 1f / (Mathf.PI * ar * Mathf.Clamp(e, 0.6f, 0.95f));

            // 最小抗力。翼型抗力の2倍に取付物の目安を足す。2倍は濡れ面積比の目安。
            w.bakedCD0 = w.airfoilCd0 * 2f + 0.002f;

            // 横力傾斜。上反角による横滑り横力を一次近似で表す。垂直尾翼の寄与は含まない。
            w.bakedCyBeta = -w.bakedLiftSlope * w.dihedralDeg * Deg * 0.25f;

            // 操舵面とフラップの効き。薄翼理論の舵 effectiveness にスパン比を掛ける。
            w.bakedClControl = w.bakedLiftSlope * FlapTau(w.controlChordRatio) * w.controlSpanFraction;
            w.bakedClFlap = w.bakedLiftSlope * FlapTau(w.flapChordRatio) * w.flapSpanFraction;
            // フラップ抗力は舵角の2乗に比例させる。係数は平板の目安。
            w.bakedCdFlap = 1.2f * w.flapSpanFraction;
            // 制動板抗力は平板抗力の目安に面積比を掛ける。揚力損失はその3割と置く。
            w.bakedCdBrake = 1.1f * w.brakeAreaRatio;
            w.bakedClBrakeLoss = 0.3f * w.brakeAreaRatio;
            // 失速時の抗力増分。はみ出し揚力係数の2乗に比例させる。
            w.bakedStallGain = 1.5f;

            // 上流翼からの吹き下ろし。遠方渦の近似で、縦距離と前後距離の減衰だけ残す。
            // 上流翼が未指定なら吹き下ろしなし・動圧比1とする。
            if (w.sourceWing != null)
            {
                FDMiWing s = w.sourceWing;
                float srcSpan = s.span;
                float srcArea = 0.5f * (s.rootChord + s.tipChord) * srcSpan;
                float srcAR = srcSpan * srcSpan / srcArea;
                float h = w.tailHeightM / srcSpan;
                float x = w.tailArmM / (0.5f * srcSpan);
                float heightGain = 1f / (1f + 4f * h * h);
                float distGain = 1f / Mathf.Sqrt(1f + x * x);
                w.bakedDownwashGain = (2f / (Mathf.PI * srcAR)) * heightGain * distGain;
                // 後流欠損。素の動圧比0.95から、上流翼の制動板面積比に応じて欠損を増やす。
                w.bakedEtaQ = 0.95f;
                w.bakedWakeLoss = 0.3f * s.brakeAreaRatio;
            }
            else
            {
                w.bakedDownwashGain = 0f;
                w.bakedEtaQ = 1f;
                w.bakedWakeLoss = 0f;
            }
        }

        // 平面フラップの effectiveness 近似。コード比から舵の効き目安を返す。
        static float FlapTau(float chordRatio)
        {
            return -0.66f * chordRatio * chordRatio + 1.35f * chordRatio + 0.15f;
        }
    }
}
