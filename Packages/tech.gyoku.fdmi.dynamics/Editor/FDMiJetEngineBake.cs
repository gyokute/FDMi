using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // FDMiJetEngine の設計値から実行時面積 (_a8/_afan/_tt4dK/_dHfD/_dHcD/_wfMax) を焼き付ける。
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
            e._dHfD = dHf;
            e._dHcD = dHc;
            float tt4 = e._tt4dK;
            float tt5 = tt4 - dHc / cp;
            float pr5 = Mathf.Pow(1f - dHc / (cp * tt4 * e.etaTurb), g / (g - 1f));
            float dHlt = (1f + e.bypassRatio) * dHf;
            float tt15 = tt5 - dHlt / cp;
            float pr15 = Mathf.Pow(1f - dHlt / (cp * tt5 * e.etaTurb), g / (g - 1f));
            float epr = pr5 * pr15 * e.burnerPR * e.corePR * e.fanPR;
            float etr = tt15 / tt0;
            // 0.75 = 圧縮機面流量関数比 Wc_face/Wc_choke (設計流量 = 0.75·Wc_choke·A_core·Pt0/√Tt0。マジックではない)
            float a8rat = 0.75f * Mathf.Sqrt(etr) / epr;
            if (a8rat > 1f)
                a8rat = 1f;
            e._a8 = a8rat * e.coreAreaM2;

            // バルブ計量スケール = 全開燃料。fuelIdle は設計入力(runtime が (fuelIdle·ign)^(1/3) で idle N2 を出す)。
            e._wfMax = SolveStaticMax(e).fuelKgS;
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
            float dHc = e._dHcD;   // N2 = 1
            float tH = 1f - dHc / (cp * tt4);
            float tt5 = tt4 * tH;
            float pr5 = Mathf.Pow(1f - (1f / e.etaTurb) * (1f - tH), adiab);
            float dHht = cp * (tt5 - tt4);

            float dHlt = -(1f + e.bypassRatio) * e._dHfD;  // N1 = 1
            float tL = 1f + dHlt / (cp * Mathf.Max(1f, tt5));
            float tt15 = tt5 * tL;
            float pr15 = Mathf.Pow(1f - (1f / e.etaTurb) * (1f - tL), adiab);

            float dHf = e._dHfD;
            float tt13 = tt2 + dHf / cp;
            float pr13 = Mathf.Pow(1f + e.etaFan * (tt13 / tt2 - 1f), adiab);
            float tt3 = tt13 + dHc / cp;
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

        internal static float WcChoke()
        {
            return 0.04041f;
        }
    }
}
