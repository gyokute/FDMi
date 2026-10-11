using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    public class FDMiJetEngine : FDMiBehaviour
    {
        public Rigidbody body;
        [Header("Input Variables")]
        // throttle 0~1
        public string throttlePath = "Throttle";

        [FDMiDataPath(nameof(throttlePath))]
        public FDMiFloat Throttle;

        // starter switch (AirSW equivalent, 0/1, optional). Adds rotation to N2.
        public string starterPath = "Starter";

        [FDMiDataPath(nameof(starterPath))]
        public FDMiBool Starter;

        // fuel switch (AirSW equivalent, 0/1, optional). Adds rotation to N2.
        public string fuelSWPath = "FuelSW";

        [FDMiDataPath(nameof(fuelSWPath))]
        public FDMiBool FuelSW;

        // Aerodynamic state
        // Mach number, optional (null = V/sqrt(gamma R T))
        public string TASPath = "TAS";

        [FDMiDataPath(nameof(TASPath))]
        public FDMiFloat TAS;

        // Mach number, optional (null = V/sqrt(gamma R T))
        public string MachPath = "Mach";

        [FDMiDataPath(nameof(MachPath))]
        public FDMiFloat Mach;

        // Static air temperature
        public string SATPath = "SAT";

        [FDMiDataPath(nameof(SATPath))]
        public FDMiFloat SAT;

        // Outside air temperature
        public string OATPath = "OAT";

        [FDMiDataPath(nameof(OATPath))]
        public FDMiFloat OAT;

        // Static air pressure
        public string StaticPressurePath = "StaticPressure";

        [FDMiDataPath(nameof(StaticPressurePath))]
        public FDMiFloat StaticPressure;

        // Outside air pressure
        public string DynamicPressurePath = "DynamicPressure";

        [FDMiDataPath(nameof(DynamicPressurePath))]
        public FDMiFloat DynamicPressure;

        // --- outputs ---
        // fuel flow [kg/s]
        public string fuelPath = "FuelFlow";

        [FDMiDataPath(nameof(fuelPath))]
        public FDMiFloat fuelFlow;

        // EGT[K]
        public string EGTPath = "EGT";

        [FDMiDataPath(nameof(EGTPath))]
        public FDMiFloat EGT;

        // fan spool
        public string n1Path = "N1";

        [FDMiDataPath(nameof(n1Path))]
        public FDMiFloat n1;

        // core spool
        public string n2Path = "N2";

        [FDMiDataPath(nameof(n2Path))]
        public FDMiFloat n2;

        #region "Design parameters"
        [Header("Spool transient (first-order lag)")]
        public float tauN2 = 0.8f; // HP spool time constant [s]
        public float tauN1 = 1.5f; // fan spool time constant [s] (follows N2)
        public float n1Idle = 0.23f; // fan idle [0,1]
        public float n2Idle = 0.55f; // core idle [0,1]
        public float starterAccel = 0.3f; // starter adds this to N2 [0,1]/s]; motoring N2 ~= starterAccel * tauN2
        public float idleThrFloor = 0.35f; // min thermal throttle with fuel on (fixed-area match inverts below)

        [Header("Engine design parameters")]
        public float fanDiameter = 2.19f;
        public float fanPR = 1.745f;
        public float OPR = 29.3f;
        public float bypassRatio = 4.4f;

        public float burnerPR = 1.0f;
        public float tt4maxC = 1096f; // burner exit at 100%

        [Header("各部の断熱圧縮効率。調整不要。")]
        public float etaFan = 1.0f;
        public float etaComp = 0.959f;
        public float etaBurn = 0.984f;
        public float etaTurb = 0.982f;
        public float etaNoz = 1.0f;

        [Header("Bake results (Editor が設定。触らない)")]
        public float corePR = 21.86f;
        public float _a4 = 0.02694f;
        public float _a4p = 0.10507f;
        public float _a8 = 0.22631f;
        public float _afan;
        public float coreAreaM2; // = 0.64707f; // 6.965 ft2
        public float _tt4dK = 1363.15f;

        #endregion

        #region "Constants"
        private float k = 273.15f; // Kelvin offset
        private float gamma = 1.4f; // polytropic index
        private float R = 287.05f; // specific gas constant for air [J/(kg K)]
        private float g0 = 9.80665f; // standard gravity [m/s^2]
        private float _fhv = 43.2e6f; // J/kg
        private float adiabaticFactor = 3.5f; // (gamma/(gamma-1))
        #endregion

        // Bake 済み値 (_tt4dK/_afan/coreAreaM2/_a4/_a4p/_a8/corePR) を使う。計算は Editor (FDMiJetEngineBake) のみ。

        void Start()
        {
            _sat = SAT.data;
            _staticPressure = StaticPressure.data;
            _mach = Mach.data;
            _oat = OAT.data;
            _dynamicPressure = DynamicPressure.data;
            _tas = TAS.data;
            _throttle = Throttle.data;
            _starter = Starter.data;
            _fuelSw = FuelSW.data;
            _fuelFlow = fuelFlow.data;
            _egt = EGT.data;
            _n1Arr = n1.data;
            _n2Arr = n2.data;
        }

        float _n1 = 0f;
        float _n2 = 0f;
        float _thrEff = 0f;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float thr = Mathf.Clamp01(_throttle[0]);

            bool fueled = _fuelSw[0];

            // N2: throttle demand + starter rotation (user requirement)
            float n2ss = n2Idle + (1.05f - n2Idle) * thr;
            _n2 += (n2ss - _n2) * (dt / (tauN2 + dt));
            if (_starter[0])
                _n2 += starterAccel * dt;
            _n2 = Mathf.Clamp(_n2, 0f, 1.05f);

            // N1 follows N2 (aero coupling proxy), never throttle directly -> two-stage lag
            float corePow = Mathf.Clamp01((_n2 - n2Idle) / Mathf.Max(1f, 1.05f - n2Idle));
            float n1ss = n1Idle + (1.05f - n1Idle) * corePow;
            _n1 += (n1ss - _n1) * (dt / (tauN1 + dt));
            _n1 = Mathf.Clamp(_n1, 0f, 1.05f);

            _n1Arr[0] = _n1;
            _n2Arr[0] = _n2;

            if (!fueled)
            {
                // dry motoring: rotation only, no combustion
                _fuelFlow[0] = 0f;
                _egt[0] = _oat[0];
                return;
            }

            _thrEff = idleThrFloor + (1f - idleThrFloor) * corePow;
            float thrust = SolveThrust();
            body.AddForceAtPosition(transform.forward * thrust, transform.position, ForceMode.Force);
        }

        float fanThrust = 0f;
        float coreTurbineThrust = 0f;

        // per-frame scratch for secant residuals (avoids arg structs)
        float[] _throttle;
        bool[] _starter;
        bool[] _fuelSw;
        float[] _sat;
        float[] _staticPressure;
        float[] _mach;
        float[] _oat;
        float[] _dynamicPressure;
        float[] _tas;
        float[] _fuelFlow;
        float[] _egt;
        float[] _n1Arr;
        float[] _n2Arr;

        float SolveThrust()
        {
            // free stream (ISA, SI)
            float ts0 = _sat[0];
            float ps0 = _staticPressure[0];
            float fmach = _mach[0];
            float tt0 = _oat[0];
            float pt0 = _dynamicPressure[0];
            float u0ms = _tas[0];

            // inlet (Mil-spec)
            float pr2 = 1f;
            if (fmach > 1f)
                pr2 = 1f - 0.075f * Mathf.Pow(fmach - 1f, 1.35f);
            float tt2 = tt0;
            float pt2 = pt0 * pr2;
            float cp2 = Cp(tt0);

            // burner temperature from throttle
            float tt4 = _tt4dK * _thrEff;
            float cp4 = Cp(tt4);

            // HPT secant: t = Tt5/Tt4
            float tH = SecantT(etaTurb, _a4p / Mathf.Max(1e-9f, _a4));
            float tt5 = tt4 * tH;
            float cp5 = Cp(tt5);
            float pr5 = Mathf.Pow(1f - (1f / etaTurb) * (1f - tH), adiabaticFactor);
            float dHht = cp4 * (tt5 - tt4); // negative

            // LPT secant: t = Tt15/Tt5
            float tL = SecantT(etaTurb, _a8 / Mathf.Max(1e-9f, _a4p));
            float tt15 = tt5 * tL;
            float pr15 = Mathf.Pow(1f - (1f / etaTurb) * (1f - tL), adiabaticFactor);
            float dHlt = cp5 * (tt15 - tt5); // negative

            // back out fan + core compressors
            float dHf = dHlt / (1f + bypassRatio);
            float tt13 = tt2 - dHf / cp2;
            if (tt13 < tt2)
                tt13 = tt2;
            float tr13 = tt13 / tt2;
            float pr13 = Mathf.Pow(1f + etaFan * (tr13 - 1f), adiabaticFactor);
            float g13 = gamma;
            float cp13 = Cp(tt13);
            float tt3 = tt13 - dHht / cp13;
            float tr3 = tt3 / Mathf.Max(1f, tt13);
            float pr3 = Mathf.Pow(1f + etaComp * (tr3 - 1f), adiabaticFactor);
            float cp3 = Cp(tt3);

            float pt13 = pt2 * pr13;
            float epr = pr15 * pr5 * burnerPR * pr3 * pr13; // PR7 = 1 (no AB)
            float etr =
                (tt15 / Mathf.Max(1f, tt5)) * (tt5 / Mathf.Max(1f, tt4)) * (tt4 / Mathf.Max(1f, tt3)) * tr3 * tr13;

            // performance: choked core flow, mdot in kg/s
            float pt8 = epr * pr2 * pt0;
            float eair = WcPerArea(1.0f) * _a8 * pt8 / Mathf.Sqrt(Mathf.Max(0.01f, etr * tt0));
            float m2t = eair * (1f + bypassRatio) * Mathf.Sqrt(tt0) / Mathf.Max(1e-6f, pr2 * pt0 * _afan);
            float m2 = SolveMach(m2t);

            // core nozzle: F = mdot*V + (pe-ps)*A
            float fac = 1.0f / adiabaticFactor;
            float npr = pt8 / Mathf.Max(1e-6f, ps0);
            float uexit = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * etr * tt0 * etaNoz * (1f - Mathf.Pow(npr, -fac)))
            );
            float pexit = npr <= 1.893f ? ps0 : 0.52828f * pt8;
            float fg = uexit + (pexit - ps0) * _a8 / Mathf.Max(1e-6f, eair);
            // fan nozzle
            float facF = 1.0f / adiabaticFactor;
            float snpr = pt13 / Mathf.Max(1e-6f, ps0);
            float ues = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, facF) * tt13 * etaNoz * (1f - Mathf.Pow(snpr, -facF)))
            );
            float pfexit = snpr <= 1.893f ? ps0 : 0.52828f * pt13;
            fg += bypassRatio * ues + (pfexit - ps0) * bypassRatio * coreAreaM2 / Mathf.Max(1e-6f, eair);

            float dram = u0ms * (1f + bypassRatio);
            float fnet = fg - dram;
            float fnN = fnet * eair;

            // fuel from energy balance
            float tr4 = tt4 / Mathf.Max(1f, tt3);
            float fa = (tr4 - 1f) / (FuelTerm(etaBurn, cp3, tt3) - tr4);
            if (fa < 0f)
                fa = 0f;

            _fuelFlow[0] = fa * eair;
            _egt[0] = tt15;
            return fnN;
        }

        float FuelTerm(float etaB, float cp3, float tt3)
        {
            return etaB * _fhv / Mathf.Max(1f, cp3 * tt3);
        }

        // secant on F(t) = area - sqrt(t)*(1-(1/eta)(1-t))^(-g/(g-1)), init (1.0, 0.5)
        private static float SecantT(float _secEta, float _secArea, float _secGamma = 1.4f)
        {
            float tPrev = 1f;
            float tCur = 0.5f;
            float fPrev = SecResid(_secEta, _secArea, tPrev);
            for (int i = 0; i < 6; i++)
            {
                float fCur = SecResid(_secEta, _secArea, tCur);
                float denom = fCur - fPrev;
                if (Mathf.Abs(denom) < 1e-7f)
                    break;
                float tNext = tCur - fCur * (tCur - tPrev) / denom;
                tNext = Mathf.Clamp(tNext, 0.2f, 1f);
                tPrev = tCur;
                fPrev = fCur;
                tCur = tNext;
                if (Mathf.Abs(fCur) < 0.001f)
                    break;
            }
            return tCur;
        }

        private static float SecResid(float _secEta, float _secArea, float t, float _secGamma = 1.4f)
        {
            float e = _secGamma / (_secGamma - 1f);
            float base_ = Mathf.Max(0.05f, 1f - (1f / _secEta) * (1f - t));
            return _secArea - Mathf.Sqrt(t) * Mathf.Pow(base_, -e);
        }

        private static float SolveMach(float target)
        {
            if (target >= WcPerArea(1.0f))
                return 1f;

            float mPrev = 0.5f;
            float mCur = 0.3f;
            float aPrev = 0.03016f;
            for (int i = 0; i < 8; i++)
            {
                float aCur = WcPerArea(mCur);
                float denom = aCur - aPrev;
                if (Mathf.Abs(denom) < 1e-7f)
                    break;
                float mNext = mCur + (target - aCur) * (mCur - mPrev) / denom;
                mNext = Mathf.Clamp(mNext, 0.01f, 1f);
                mPrev = mCur;
                aPrev = aCur;
                mCur = mNext;
            }
            return mCur;
        }

        // corrected flow per area: mdot*sqrt(Tt)/(Pt*A), SI
        private static float WcPerArea(float m)
        {
            if (m == 1f)
                return 0.04041f;
            else
                return 0.06984f * m / Mathf.Pow(1f + 0.2f * m * m, 3f);
        }

        // Cp fit of EngineSim-UA AeroUtil, Rankine poly remapped to J/(kg K), Kelvin input
        private static float Cp(float tK)
        {
            return 1005f;
            // float t2 = tK * tK;
            // float t3 = t2 * tK;
            // return -1.09151e-08f * t3 - 6.95718e-06f * t2 + 0.2134518f * tK + 940.055f;
        }
    }
}
