using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 高度・Mach 指定の全開推力予測 (Editor 専用)。
    // 大気 (SAT/TAT・静圧/全圧・TAS) は FDMiJetEngineSimUA (EngineSim-UA) の ISA 式で推算し、
    // 推力は FDMiJetEngine.SolveThrust() と同式 (熱スロットル全開) で解く。
    // ランタイムには一切触れない。SI 単位系。
    public static class FDMiJetEngineEnvelope
    {
        public struct Atmo
        {
            public float satK; // 静温 [K]
            public float tatK; // 全温 [K]
            public float psPa; // 静圧 [Pa]
            public float ptPa; // 全圧 [Pa]
            public float tasMS; // 真対気速 [m/s]
        }

        public struct FullPower
        {
            public float thrustN;
            public float epr;
        }

        // SimUA SolveThrustLbf の free-stream 部 (ISA・English) を SI に直した推算。
        public static Atmo SampleAtmosphere(float altM, float mach)
        {
            float altFt = altM * 3.28084f;
            float ts0R;
            float ps0psf;
            if (altFt < 36152f)
            {
                ts0R = 518.6f - 3.56f * altFt / 1000f;
                ps0psf = 2116f * Mathf.Pow(ts0R / 518.6f, 5.256f);
            }
            else if (altFt <= 82345f)
            {
                ts0R = 389.98f;
                ps0psf = 2116f * 0.2236f * Mathf.Exp((36000f - altFt) / (53.35f * 389.98f));
            }
            else
            {
                ts0R = 389.98f + 1.645f * (altFt - 82345f) / 1000f;
                ps0psf = 2116f * 0.02456f * Mathf.Pow(ts0R / 389.98f, -11.388f);
            }
            float gam0 = GammaR(ts0R);
            float a0fts = Mathf.Sqrt(gam0 * 1718f * ts0R);
            float u0fts = mach * a0fts;
            float tt0R = ts0R * (1f + 0.5f * (gam0 - 1f) * mach * mach);
            float pt0psf = ps0psf * Mathf.Pow(tt0R / ts0R, gam0 / (gam0 - 1f));

            Atmo a = new Atmo();
            a.satK = ts0R * 5f / 9f;
            a.tatK = tt0R * 5f / 9f;
            a.psPa = ps0psf * 47.8802589f;
            a.ptPa = pt0psf * 47.8802589f;
            a.tasMS = u0fts * 0.3048f;
            return a;
        }

        // ランタイム SolveThrust() と同式。thrEff = 1 (熱スロットル全開)。
        public static FullPower SolveFullPower(FDMiJetEngine e, float altM, float mach)
        {
            if (mach < 0f)
                mach = 0f;
            Atmo a = SampleAtmosphere(altM, mach);

            float R = 287.05f;
            float cp = 1005f;
            float adiab = 3.5f;

            float pr2 = 1f;
            if (mach > 1f)
                pr2 = 1f - 0.075f * Mathf.Pow(mach - 1f, 1.35f);
            float tt2 = a.tatK;
            float pt2 = a.ptPa * pr2;

            float tt4 = e._tt4dK;
            float dHf = e._dHfD;   // N1 = 1
            float tt13 = tt2 + dHf / cp;
            float tr13 = tt13 / tt2;
            float pr13 = Mathf.Pow(1f + e.etaFan * (tr13 - 1f), adiab);
            float dHc = e._dHcD;   // N2 = 1
            float tt3 = tt13 + dHc / cp;
            float tr3 = tt3 / Mathf.Max(1f, tt13);
            float pr3 = Mathf.Pow(1f + e.etaComp * (tr3 - 1f), adiab);

            float tH = 1f - dHc / (cp * tt4);       // HPT は HPC を駆動
            float tt5 = tt4 * tH;
            float pr5 = Mathf.Pow(1f - (1f / e.etaTurb) * (1f - tH), adiab);

            float dHlt = -(1f + e.bypassRatio) * dHf;  // LPT はファン(+バイパス)を駆動
            float tL = 1f + dHlt / (cp * Mathf.Max(1f, tt5));
            float tt15 = tt5 * tL;
            float pr15 = Mathf.Pow(1f - (1f / e.etaTurb) * (1f - tL), adiab);

            float pt13 = pt2 * pr13;
            float epr = pr15 * pr5 * e.burnerPR * pr3 * pr13;
            float etr =
                (tt15 / Mathf.Max(1f, tt5))
                * (tt5 / Mathf.Max(1f, tt4))
                * (tt4 / Mathf.Max(1f, tt3))
                * tr3
                * tr13;

            float pt8 = epr * pr2 * a.ptPa;
            float eair = FDMiJetEngineBake.WcChoke() * e._a8 * pt8 / Mathf.Sqrt(Mathf.Max(0.01f, etr * a.tatK));

            float fac = 1f / adiab;
            float npr = pt8 / Mathf.Max(1e-6f, a.psPa);
            float uexit = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * etr * a.tatK * e.etaNoz * (1f - Mathf.Pow(npr, -fac)))
            );
            float pexit = npr <= 1.893f ? a.psPa : 0.52828f * pt8;
            float fg = uexit + (pexit - a.psPa) * e._a8 / Mathf.Max(1e-6f, eair);

            float snpr = pt13 / Mathf.Max(1e-6f, a.psPa);
            float ues = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * tt13 * e.etaNoz * (1f - Mathf.Pow(snpr, -fac)))
            );
            float pfexit = snpr <= 1.893f ? a.psPa : 0.52828f * pt13;
            fg += e.bypassRatio * ues + (pfexit - a.psPa) * e.bypassRatio * e.coreAreaM2 / Mathf.Max(1e-6f, eair);

            float dram = a.tasMS * (1f + e.bypassRatio);
            float fnN = (fg - dram) * eair;
            if (fnN < 0f)
                fnN = 0f;

            FullPower r = new FullPower();
            r.thrustN = fnN;
            r.epr = epr;
            return r;
        }

        // SimUA AeroUtil Gamma (Rankine 入力)。大気推算のみに使う。
        static float GammaR(float tR)
        {
            float t2 = tR * tR;
            float t3 = t2 * tR;
            return -7.6942651e-13f * t3 + 1.3764661e-08f * t2 - 7.8185709e-05f * tR + 1.436914f;
        }
    }
}
