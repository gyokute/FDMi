using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // ホイールの設計値から実行時係数を焼き付ける。Editor 側の通常 C# で動くため、
    // Sqrt・Clamp を自由に使える。Udon の制約は受けない。
    // 構造的不変条件の正規化 (折点が静姿勢より上、ABS 閾値順序、muSlide クランプ、
    // 静荷重由来の基本レート・末端レート・臨界減衰・脚上限・各種逆数) をここに移す。
    // 実行時は焼き付け済み係数を読み出すだけ。Pow はどこにもない。
    // FDMiWheelAutoSetupUseCase がホイールを選ぶたびに呼ぶため、値が変わったかを返す。
    // 変わっていないのに true を返すと、選択しただけでシーンが dirty になる。
    public static class FDMiWheelBake
    {
        public static bool Bake(FDMiWheelCollider w)
        {
            bool changed = false;

            // 構造的不変条件の正規化。シーンデータ自体を直す (Wing の Bake と同じ流儀)。
            if (w.muSlide > w.muPeak)
                changed |= Assign(ref w.muSlide, w.muPeak);
            if (w.absLowSlip >= w.absHighSlip)
                changed |= Assign(ref w.absLowSlip, w.absHighSlip * 0.5f);

            float mass = w.body != null ? w.body.mass : 200000f;
            if (mass < 1f)
                mass = 1f;
            changed |= Assign(ref w.bakedBodyMass, mass);

            float wi = Mathf.Max(w.loadShare, 0.001f);
            float target = mass * 9.81f * wi;
            changed |= Assign(ref w.bakedFzCap, w.maxLoadFactor * target);

            // 基本レートは静姿勢で定義通りに決まる。ガスプリロード解法は不要。
            float xs = Mathf.Clamp(w.staticStroke, 0.01f, w.maxStroke * 0.95f);
            float k0 = target / xs;
            changed |= Assign(ref w.bakedK0, k0);

            // 折点は静姿勢より下がれない。下がっていたら静姿勢まで引き上げる。
            float kneeX = w.kneeFraction * w.maxStroke;
            if (kneeX < xs)
            {
                kneeX = xs;
                changed |= Assign(ref w.kneeFraction, kneeX / w.maxStroke);
            }
            changed |= Assign(ref w.bakedKneeX, kneeX);
            changed |= Assign(ref w.bakedKEnd, k0 * Mathf.Max(w.endRateFactor, 1f));

            // 減衰は臨界減衰比で指定し、実値はここで解く。分担質量に対する値。
            float cCrit = 2f * Mathf.Sqrt(k0 * mass * wi);
            changed |= Assign(ref w.bakedCCompress, Mathf.Max(w.zetaCompress, 0f) * cCrit);
            changed |= Assign(ref w.bakedCRebound, Mathf.Max(w.zetaRebound, 0f) * cCrit);

            float alphaP = Mathf.Max(w.alphaPeakDeg, 0.5f) * Mathf.Deg2Rad;
            changed |= Assign(ref w.bakedAlphaP, alphaP);
            changed |= Assign(ref w.bakedInvR, 1f / w.tireRadius);
            float kp = Mathf.Max(w.kappaPeak, 1e-4f);
            changed |= Assign(ref w.bakedInvKappaPeak, 1f / kp);
            changed |= Assign(ref w.bakedInvSlideSpan, 1f / Mathf.Max(1f - w.kappaPeak, 1e-4f));
            changed |= Assign(ref w.bakedInvAlphaP, 1f / alphaP);
            changed |= Assign(ref w.bakedInvMaxStroke, 1f / w.maxStroke);
            changed |= Assign(ref w.bakedMuSpan, w.muSlide - w.muPeak);
            changed |= Assign(ref w.bakedMuCap, Mathf.Max(w.muPeak, w.muLateral));
            changed |= Assign(ref w.bakedKc, Mathf.Clamp(w.kappaCoupling, 0f, 1f));

            return changed;
        }

        // 値が変わったときだけ書く。焼き付けを選択のたびに回すための差分検出。
        static bool Assign(ref float field, float value)
        {
            if (field == value)
                return false;
            field = value;
            return true;
        }
    }
}
