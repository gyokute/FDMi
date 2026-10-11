using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // FDMiJetEngine の設計値から実行時面積 (_a4/_a4p/_a8/_afan/_tt4dK) を焼き付ける。
    // Editor 側の通常 C# で動くため、Udon の制約は受けない。
    // ランタイムに SizeAreas() はない。設計値を変えたら Editor が自動で焼き直す。
    // 静止最大推力 = ISA SL (15℃, 101.325kPa)・速度 0・スロットル全開の推力
    // (離陸定格の表示条件。調査メモ: Docs/JetEngineStaticThrust.md)。
    public static class FDMiJetEngineBake
    {
        public struct StaticResult
        {
            public float thrustN;
            public float epr;
            public float fuelKgS;
            public float airKgS;
            public float fa;
        }

        public static void Bake(FDMiJetEngine e)
        {
            float g = 1.4f;
            float cp = 1005f;

            // OPR = fanPR * corePR (コア流は fan→core の直列圧縮)。OPR と fanPR から corePR を導く。
            e.corePR = e.OPR / e.fanPR;

            // fan 直径と BPR からコア面積・fan 面積を導く。
            float fanArea = Mathf.PI * Mathf.Pow(e.fanDiameter * 0.5f, 2f);
            e.coreAreaM2 = fanArea / (1f + e.bypassRatio);
            e._afan = fanArea;

            e._tt4dK = e.tt4maxC + 273.15f;

            // ---- design-mode pass (SL 静止・全開の閉形式。SimUA SizeAreas の移植) ----
            float tt0 = 288.15f;
            float dHf = cp * tt0 / e.etaFan * (Mathf.Pow(e.fanPR, (g - 1f) / g) - 1f);
            float tt13 = tt0 + dHf / cp;
            float dHc = cp * tt13 / e.etaComp * (Mathf.Pow(e.corePR, (g - 1f) / g) - 1f);
            float tt3 = tt13 + dHc / cp;
            float tt4 = e._tt4dK;
            float tr4 = tt4 / tt3;
            float tt5 = tt4 - dHc / cp;
            float pr5 = Mathf.Pow(1f - dHc / (cp * tt4 * e.etaTurb), g / (g - 1f));
            float dHlt = (1f + e.bypassRatio) * dHf;
            float tt15 = tt5 - dHlt / cp;
            float pr15 = Mathf.Pow(1f - dHlt / (cp * tt5 * e.etaTurb), g / (g - 1f));
            float tr5 = tt5 / tt4;
            float tr15 = tt15 / tt5;
            float tr13 = tt13 / tt0;
            float tr3 = tt3 / tt13;
            float epr = pr5 * pr15 * e.burnerPR * e.corePR * e.fanPR;
            float etr = tr5 * tr15 * tr4 * tr3 * tr13;
            float a8rat = 0.75f * Mathf.Sqrt(etr) / epr;
            if (a8rat > 1f)
                a8rat = 1f;
            e._a8 = a8rat * e.coreAreaM2;
            e._a4 = e._a8 * pr5 * pr15 / Mathf.Sqrt(tr5 * tr15);
            e._a4p = e._a8 * pr15 / Mathf.Sqrt(tr15);
        }

        // ISA SL・速度0・全開の1点 solve。ランタイム SolveThrust() と同式。
        public static StaticResult SolveStaticMax(FDMiJetEngine e)
        {
            float R = 287.05f;
            float cp = 1005f;
            float fhv = 43.2e6f; // J/kg
            float adiab = 3.5f;

            float ts0 = 288.15f;
            float ps0 = 101325f;
            float tt0 = ts0;
            float pt0 = ps0;
            float tt2 = tt0;
            float pt2 = pt0;

            float tt4 = e._tt4dK;
            float tH = SecantT(e.etaTurb, e._a4p / Mathf.Max(1e-9f, e._a4));
            float tt5 = tt4 * tH;
            float pr5 = Mathf.Pow(1f - (1f / e.etaTurb) * (1f - tH), adiab);
            float dHht = cp * (tt5 - tt4);

            float tL = SecantT(e.etaTurb, e._a8 / Mathf.Max(1e-9f, e._a4p));
            float tt15 = tt5 * tL;
            float pr15 = Mathf.Pow(1f - (1f / e.etaTurb) * (1f - tL), adiab);
            float dHlt = cp * (tt15 - tt5);

            float dHf = dHlt / (1f + e.bypassRatio);
            float tt13 = tt2 - dHf / cp;
            if (tt13 < tt2)
                tt13 = tt2;
            float pr13 = Mathf.Pow(1f + e.etaFan * (tt13 / tt2 - 1f), adiab);
            float tt3 = tt13 - dHht / cp;
            float tr3 = tt3 / Mathf.Max(1f, tt13);
            float pr3 = Mathf.Pow(1f + e.etaComp * (tr3 - 1f), adiab);
            float cp3 = cp;

            float pt13 = pt2 * pr13;
            float epr = pr15 * pr5 * e.burnerPR * pr3 * pr13;
            float etr =
                (tt15 / Mathf.Max(1f, tt5))
                * (tt5 / Mathf.Max(1f, tt4))
                * (tt4 / Mathf.Max(1f, tt3))
                * tr3
                * (tt13 / tt2);

            float pt8 = epr * pt0;
            float eair = WcChoke() * e._a8 * pt8 / Mathf.Sqrt(Mathf.Max(0.01f, etr * tt0));

            float fac = 1f / adiab;
            float npr = pt8 / Mathf.Max(1e-6f, ps0);
            float uexit = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * etr * tt0 * e.etaNoz * (1f - Mathf.Pow(npr, -fac)))
            );
            float pexit = npr <= 1.893f ? ps0 : 0.52828f * pt8;
            float fg = uexit + (pexit - ps0) * e._a8 / Mathf.Max(1e-6f, eair);

            float snpr = pt13 / Mathf.Max(1e-6f, ps0);
            float ues = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * tt13 * e.etaNoz * (1f - Mathf.Pow(snpr, -fac)))
            );
            float pfexit = snpr <= 1.893f ? ps0 : 0.52828f * pt13;
            fg += e.bypassRatio * ues + (pfexit - ps0) * e.bypassRatio * e.coreAreaM2 / Mathf.Max(1e-6f, eair);

            float tr4 = tt4 / Mathf.Max(1f, tt3);
            float fa = (tr4 - 1f) / (e.etaBurn * fhv / Mathf.Max(1f, cp3 * tt3) - tr4);
            if (fa < 0f)
                fa = 0f;

            StaticResult r = new StaticResult();
            r.thrustN = fg * eair;
            r.epr = epr;
            r.fuelKgS = fa * eair;
            r.airKgS = eair;
            r.fa = fa;
            return r;
        }

        static float SecantT(float eta, float area)
        {
            float tPrev = 1f;
            float tCur = 0.5f;
            float fPrev = SecResid(eta, area, tPrev);
            for (int i = 0; i < 6; i++)
            {
                float fCur = SecResid(eta, area, tCur);
                float denom = fCur - fPrev;
                if (Mathf.Abs(denom) < 1e-7f)
                    break;
                float tNext = Mathf.Clamp(tCur - fCur * (tCur - tPrev) / denom, 0.2f, 1f);
                tPrev = tCur;
                fPrev = fCur;
                tCur = tNext;
                if (Mathf.Abs(fCur) < 0.001f)
                    break;
            }
            return tCur;
        }

        static float SecResid(float eta, float area, float t)
        {
            float e = 1.4f / 0.4f;
            float b = Mathf.Max(0.05f, 1f - (1f / eta) * (1f - t));
            return area - Mathf.Sqrt(t) * Mathf.Pow(b, -e);
        }

        static float WcChoke()
        {
            return 0.04041f;
        }
    }
}
