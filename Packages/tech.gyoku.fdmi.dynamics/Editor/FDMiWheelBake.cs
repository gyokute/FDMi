using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // ホイールの設計値から実行時係数を焼き付ける。Editor 側の通常 C# で動くため、
    // Sqrt・Clamp を自由に使える。Udon の制約は受けない。
    // 構造的不変条件の正規化 (折点が静姿勢より上、ABS 閾値順序、muSlide クランプ、
    // 静荷重由来の基本レート・末端レート・臨界減衰・脚上限・各種逆数) をここに移す。
    // 実行時は焼き付け済み係数を読み出すだけ。Pow はどこにもない。
    // 設計値・body.mass を変えたら押し直す。
    public static class FDMiWheelBake
    {
        public static void Bake(FDMiWheelCollider w)
        {
            // 構造的不変条件の正規化。シーンデータ自体を直す (Wing の Bake と同じ流儀)。
            if (w.muSlide > w.muPeak)
                w.muSlide = w.muPeak;
            if (w.absLowSlip >= w.absHighSlip)
                w.absLowSlip = w.absHighSlip * 0.5f;

            float mass = w.body != null ? w.body.mass : 200000f;
            if (mass < 1f)
                mass = 1f;
            w.bakedBodyMass = mass;

            float wi = Mathf.Max(w.loadShare, 0.001f);
            float target = mass * 9.81f * wi;
            w.bakedFzCap = w.maxLoadFactor * target;

            // 基本レートは静姿勢で定義通りに決まる。ガスプリロード解法は不要。
            float xs = Mathf.Clamp(w.staticStroke, 0.01f, w.maxStroke * 0.95f);
            w.bakedK0 = target / xs;

            // 折点は静姿勢より下がれない。下がっていたら静姿勢まで引き上げる。
            float kneeX = w.kneeFraction * w.maxStroke;
            if (kneeX < xs)
            {
                kneeX = xs;
                w.kneeFraction = kneeX / w.maxStroke;
            }
            w.bakedKneeX = kneeX;
            w.bakedKEnd = w.bakedK0 * Mathf.Max(w.endRateFactor, 1f);

            // 減衰は臨界減衰比で指定し、実値はここで解く。分担質量に対する値。
            float cCrit = 2f * Mathf.Sqrt(w.bakedK0 * mass * wi);
            w.bakedCCompress = Mathf.Max(w.zetaCompress, 0f) * cCrit;
            w.bakedCRebound = Mathf.Max(w.zetaRebound, 0f) * cCrit;

            w.bakedAlphaP = Mathf.Max(w.alphaPeakDeg, 0.5f) * Mathf.Deg2Rad;
            w.bakedInvR = 1f / w.tireRadius;
            float kp = Mathf.Max(w.kappaPeak, 1e-4f);
            w.bakedInvKappaPeak = 1f / kp;
            w.bakedInvSlideSpan = 1f / Mathf.Max(1f - w.kappaPeak, 1e-4f);
            w.bakedInvAlphaP = 1f / w.bakedAlphaP;
            w.bakedInvMaxStroke = 1f / w.maxStroke;
            w.bakedMuSpan = w.muSlide - w.muPeak;
            w.bakedMuCap = Mathf.Max(w.muPeak, w.muLateral);
            w.bakedKc = Mathf.Clamp(w.kappaCoupling, 0f, 1f);
        }
    }
}
